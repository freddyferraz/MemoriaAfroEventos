/* Planos para organizadores */
(function () {
    const el = (id) => document.getElementById(id);
    let atual = null;

    function cartao(p) {
        const eAtual = atual && atual.plano.id === p.id;
        let botao;
        if (p.precoMensal === 0) botao = '<span class="chip-status">Incluído na conta</span>';
        else if (!Session.logado()) botao = '<a class="btn btn-outline" href="login.html">Entrar para contratar</a>';
        else botao = `<button class="btn btn-primary" data-plano="${p.id}">${eAtual ? "Renovar por 30 dias" : "Contratar"}</button>`;
        return `
            <div class="plano-card ${eAtual ? "atual" : ""}">
                <h3>${escapeHtml(p.nome)} ${eAtual ? '<span class="chip-status">Seu plano</span>' : ""}</h3>
                <div class="plano-preco">${p.precoMensal === 0 ? "Grátis" : Loja.moeda(p.precoMensal)}${p.precoMensal === 0 ? "" : " <small>por 30 dias</small>"}</div>
                <ul><li>${p.destaquesPorMes} ${p.destaquesPorMes === 1 ? "divulgação em destaque" : "divulgações em destaque"} por mês</li></ul>
                ${botao}
            </div>`;
    }

    async function carregar() {
        try {
            if (Session.logado()) {
                try { atual = await Api.meuPlano(); } catch { atual = null; }
            }
            if (atual) {
                const ate = atual.validaAte ? `, válido até ${Loja.data(atual.validaAte)}` : "";
                el("meu-plano").innerHTML = `<div class="cota">Seu plano: <strong>${escapeHtml(atual.plano.nome)}</strong>${ate}. Destaques neste mês: <strong>${atual.cota.usado} de ${atual.cota.limite}</strong> usados (${atual.cota.disponivel} disponível(is)).</div>`;
            }
            const planos = await Api.listarPlanos();
            el("planos").innerHTML = planos.map(cartao).join("");
        } catch { el("planos").innerHTML = EventosUI.vazio("Não foi possível carregar os planos."); }
    }

    document.addEventListener("DOMContentLoaded", () => {
        carregar();
        el("planos").addEventListener("click", async (ev) => {
            const botao = ev.target.closest("[data-plano]");
            if (!botao) return;
            botao.disabled = true;
            try { const r = await Api.contratarPlano(Number(botao.dataset.plano)); location.href = r.urlPagamento; }
            catch (e) { mostrarToast(e.message, "erro"); botao.disabled = false; }
        });
    });
})();
