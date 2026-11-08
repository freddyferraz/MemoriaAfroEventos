/* Recomendações: "Recomendado para você" (início, só para quem está logado) e "Você também pode gostar" (detalhes do evento). */
(function () {
    const q = (id) => document.getElementById(id);
    let escolhas = { categorias: [], cidades: [] };

    function cartao(e) {
        return `<div>${EventosUI.card(e)}<p class="rec-motivo">${escapeHtml(e.motivo)}</p></div>`;
    }

    function mostrarVisitante() {
        q("para-voce-visitante").hidden = false;
        q("para-voce-conteudo").hidden = true;
    }

    // ---------- Início (logado) ----------
    async function carregarParaVoce() {
        const grade = q("para-voce-grid");
        const sinais = Interesses.sinais();
        const temEscolhas = escolhas.categorias.length > 0 || escolhas.cidades.length > 0;
        q("para-voce-info").textContent = temEscolhas || Interesses.temSinais()
            ? "Com base nos interesses que você escolheu e nos eventos que você abriu."
            : "Escolha seus interesses abaixo para receber sugestões. Enquanto isso, mostramos os destaques e o que acontece em breve.";
        try {
            const itens = await Api.recomendar({ categorias: sinais.categorias, cidades: sinais.cidades, limite: 8 });
            grade.innerHTML = itens.length ? itens.map(cartao).join("") : EventosUI.vazio("Ainda não há eventos para sugerir.");
        } catch (e) {
            if (e.status === 401) { Session.limpar(); mostrarVisitante(); return; } // sessão expirada
            grade.innerHTML = EventosUI.vazio("Não foi possível carregar as sugestões.");
        }
    }

    function desenharChips(categorias) {
        const nomes = escolhas.categorias.map((id) => categorias.find((c) => c.id === id)?.nome).filter(Boolean).concat(escolhas.cidades);
        q("para-voce-chips").innerHTML = nomes.map((n) => `<span class="chip">${escapeHtml(n)}</span>`).join("");
    }

    async function montarPainel() {
        let categorias = [], cidades = [];
        try {
            [categorias, cidades, escolhas] = await Promise.all([Api.listarCategoriasEvento(), Api.listarCidadesEvento(), Api.meusInteresses()]);
        } catch (e) {
            if (e.status === 401) { Session.limpar(); mostrarVisitante(); return; }
        }

        q("int-categorias").innerHTML = categorias.map((c) =>
            `<label><input type="checkbox" value="${c.id}" ${escolhas.categorias.includes(c.id) ? "checked" : ""} /> ${escapeHtml(c.nome)}</label>`).join("");
        if (cidades.length) {
            q("int-cidades").innerHTML = cidades.map((c) =>
                `<label><input type="checkbox" value="${escapeHtml(c)}" ${escolhas.cidades.includes(c) ? "checked" : ""} /> ${escapeHtml(c)}</label>`).join("");
            q("int-bloco-cidades").hidden = false;
        }
        desenharChips(categorias);
        carregarParaVoce();

        const marcados = (id) => [...q(id).querySelectorAll("input:checked")].map((i) => i.value);
        q("int-salvar").addEventListener("click", async () => {
            const novas = { categorias: marcados("int-categorias").map(Number), cidades: marcados("int-cidades") };
            try {
                await Api.salvarInteresses(novas);
                escolhas = novas;
                desenharChips(categorias);
                q("painel-interesses").open = false;
                mostrarToast("Interesses salvos na sua conta.");
                carregarParaVoce();
            } catch (e) { mostrarToast(e.message, "erro"); }
        });
        q("int-limpar").addEventListener("click", async () => {
            try {
                await Api.salvarInteresses({ categorias: [], cidades: [] });
                Interesses.limparSinais();
                escolhas = { categorias: [], cidades: [] };
                q("painel-interesses").querySelectorAll("input").forEach((i) => { i.checked = false; });
                desenharChips(categorias);
                mostrarToast("Interesses apagados.");
                carregarParaVoce();
            } catch (e) { mostrarToast(e.message, "erro"); }
        });
    }

    // ---------- Detalhes do evento ----------
    document.addEventListener("evento-carregado", async (ev) => {
        const e = ev.detail;
        Interesses.registrar(e, 1);

        const alvo = q("extra-similares");
        if (!alvo) return;
        try {
            const itens = await Api.eventosSimilares(e.id, 4);
            if (!itens.length) return;
            alvo.innerHTML = `<section class="extra"><h2>Você também pode gostar</h2><div class="ev-grid">${itens.map(cartao).join("")}</div></section>`;
        } catch { /* a seção simplesmente não aparece */ }
    });

    document.addEventListener("DOMContentLoaded", () => {
        if (!q("para-voce")) return;
        if (!Session.logado()) { mostrarVisitante(); return; }
        q("para-voce-conteudo").hidden = false;
        montarPainel();
    });
})();
