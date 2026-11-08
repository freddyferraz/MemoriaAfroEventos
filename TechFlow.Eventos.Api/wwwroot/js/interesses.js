/* Sinais de interesse da pessoa logada.
   Os interesses ESCOLHIDOS ficam na conta (servidor). Aqui ficam só os sinais de uso (abrir um evento, adicionar à agenda,
   avaliar bem), guardados neste navegador e separados por conta. Sem login, nada é registrado. */
const Interesses = (() => {
    const PREFIXO = "cc.sinais.";
    localStorage.removeItem("cc.interesses"); // versão antiga, anterior às contas

    const chave = () => {
        const a = Session.obter();
        return a?.email ? PREFIXO + a.email.toLowerCase() : null;
    };
    const vazio = () => ({ vistas: {}, cidadesVistas: {} });

    function ler() {
        const k = chave();
        if (!k) return vazio();
        try { return { ...vazio(), ...JSON.parse(localStorage.getItem(k) || "{}") }; } catch { return vazio(); }
    }

    return {
        logado: () => !!chave(),
        /** Sinais no formato esperado pela API de recomendações. */
        sinais() {
            const p = ler();
            return {
                categorias: Object.entries(p.vistas).map(([categoria, peso]) => ({ categoria: Number(categoria), peso })),
                cidades: Object.entries(p.cidadesVistas).filter(([, n]) => n >= 2).map(([c]) => c),
            };
        },
        registrar(evento, peso = 1) {
            const k = chave();
            if (!k || !evento || peso <= 0) return;
            const p = ler();
            p.vistas[evento.categoria] = (p.vistas[evento.categoria] || 0) + peso;
            p.cidadesVistas[evento.cidade] = (p.cidadesVistas[evento.cidade] || 0) + peso;
            try { localStorage.setItem(k, JSON.stringify(p)); } catch { /* armazenamento indisponível */ }
        },
        limparSinais() {
            const k = chave();
            if (k) localStorage.removeItem(k);
        },
        temSinais() {
            const s = this.sinais();
            return s.categorias.length > 0 || s.cidades.length > 0;
        },
    };
})();
