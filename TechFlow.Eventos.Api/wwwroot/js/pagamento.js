/* Tela de pagamento (modo Simulado ou Manual) */
(function () {
    const el = (id) => document.getElementById(id);
    const ref = new URLSearchParams(location.search).get("ref");

    function destino(p) { return p.tipo === 1 ? ["meus-pedidos.html", "Ver meus pedidos"] : ["planos.html", "Ver meu plano"]; }

    async function carregar() {
        const alvo = el("conteudo");
        if (!Session.logado()) { alvo.innerHTML = Loja.convidarLogin("Entre para ver este pagamento."); return; }
        if (!ref) { alvo.innerHTML = EventosUI.vazio("Pagamento não informado."); return; }
        try {
            const p = await Api.obterPagamento(ref);
            const [href, rotulo] = destino(p);
            if (p.status === 2) {
                alvo.innerHTML = `<div class="msg-box"><h2>Pagamento confirmado</h2><p>${escapeHtml(p.descricao)}</p><a class="btn btn-primary" href="${href}">${rotulo}</a></div>`;
                return;
            }
            if (p.status === 3) { alvo.innerHTML = `<div class="msg-box erro"><p>Este pagamento foi cancelado.</p><a class="btn btn-outline" href="${href}">${rotulo}</a></div>`; return; }

            const simulado = p.simulavel
                ? '<div class="aviso"><strong>Pagamento simulado.</strong> Isto é uma demonstração: nenhum valor real é cobrado. Use o botão para aprovar.</div>' : "";
            const manual = p.instrucoes
                ? `<div class="aviso" style="white-space:pre-line"><strong>Como pagar</strong>\n${escapeHtml(p.instrucoes)}\nCitar a referência: ${escapeHtml(p.referencia)}</div>` : "";
            alvo.innerHTML = `
                <div class="form-panel">
                    <p style="margin:0 0 4px">${escapeHtml(p.descricao)}</p>
                    <p class="prod-preco" style="font-size:2rem">${Loja.moeda(p.valor)}</p>
                    ${simulado}${manual}
                    ${p.simulavel ? '<button class="btn btn-primary btn-block" id="btn-simular">Simular pagamento aprovado</button>'
                        : '<p class="form-hint">Assim que o pagamento for confirmado, esta página e a sua conta serão atualizadas.</p><a class="btn btn-outline btn-block" href="">Atualizar situação</a>'}
                    <p class="erro-form" id="erro" role="alert"></p>
                </div>`;
            el("btn-simular")?.addEventListener("click", async (ev) => {
                ev.target.disabled = true;
                try { await Api.simularPagamento(ref); carregar(); }
                catch (e) { el("erro").textContent = e.message; ev.target.disabled = false; }
            });
        } catch (e) { alvo.innerHTML = EventosUI.vazio(e.status === 404 ? "Pagamento não encontrado." : escapeHtml(e.message)); }
    }

    document.addEventListener("DOMContentLoaded", carregar);
})();
