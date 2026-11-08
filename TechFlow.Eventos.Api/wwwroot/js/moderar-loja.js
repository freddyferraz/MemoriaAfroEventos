/* Moderação da loja (produtos) e confirmação manual de pagamentos (somente Admin). */
(function () {
    const el = (id) => document.getElementById(id);
    const ehAdmin = () => !!Session.obter()?.roles?.includes("Admin");

    async function carregarProdutos() {
        const alvo = el("pendentes-produtos");
        if (!alvo || !Session.obter()) return;
        try {
            const lista = await Api.produtosPendentes();
            if (!lista.length) { alvo.innerHTML = EventosUI.vazio("Nenhum produto aguardando aprovação."); return; }
            alvo.innerHTML = lista.map((p) => `
                <div class="mod-item" data-id="${p.id}">
                    <div>
                        <h3 style="margin-bottom:4px">${escapeHtml(p.titulo)}</h3>
                        <div class="ev-meta">${Loja.moeda(p.preco)}, estoque ${p.estoque}, ${escapeHtml(p.categoriaNome)}, ${escapeHtml(p.cidade)}. Vendedor: ${escapeHtml(p.vendedorNome)}</div>
                        ${p.temFoto ? `<img class="mod-foto" data-foto="${p.id}" alt="Foto de ${escapeHtml(p.titulo)}" />` : ""}
                        <p style="margin:8px 0 0;white-space:pre-line">${escapeHtml(p.descricao)}</p>
                    </div>
                    <div class="mod-acoes">
                        <button class="btn btn-primary btn-small" data-aprovar="1">Aprovar</button>
                        <button class="btn btn-small btn-perigo" data-aprovar="0">Rejeitar</button>
                    </div>
                </div>`).join("");
            alvo.querySelectorAll("[data-foto]").forEach(async (img) => {
                try {
                    const auth = Session.obter();
                    const r = await fetch(Loja.fotoUrl(img.dataset.foto), { headers: { Authorization: `Bearer ${auth.token}` } });
                    if (!r.ok) throw new Error();
                    img.src = URL.createObjectURL(await r.blob());
                } catch { img.remove(); }
            });
        } catch (e) { alvo.innerHTML = EventosUI.vazio(e.status === 403 ? "Esta conta não tem permissão de moderador." : escapeHtml(e.message)); }
    }

    async function carregarPagamentos() {
        const alvo = el("pendentes-pagamentos");
        if (!alvo || !ehAdmin()) return;
        el("titulo-pagamentos").hidden = false;
        try {
            const lista = await Api.pagamentosPendentes();
            if (!lista.length) { alvo.innerHTML = EventosUI.vazio("Nenhum pagamento aguardando confirmação."); return; }
            alvo.innerHTML = lista.map((p) => `
                <div class="mod-item" data-ref="${escapeHtml(p.referencia)}">
                    <div>
                        <h3 style="margin-bottom:4px">${escapeHtml(p.descricao)}</h3>
                        <div class="ev-meta">${Loja.moeda(p.valor)}, ${escapeHtml(p.usuarioNome)}, ${Loja.data(p.criadoEm)}, referência ${escapeHtml(p.referencia)} (${escapeHtml(p.gateway)})</div>
                    </div>
                    <div class="mod-acoes"><button class="btn btn-primary btn-small" data-confirmar>Confirmar recebimento</button></div>
                </div>`).join("");
        } catch (e) { alvo.innerHTML = EventosUI.vazio(escapeHtml(e.message)); }
    }

    document.addEventListener("DOMContentLoaded", () => {
        carregarProdutos();
        carregarPagamentos();

        el("pendentes-produtos")?.addEventListener("click", async (ev) => {
            const botao = ev.target.closest("[data-aprovar]");
            if (!botao) return;
            botao.disabled = true;
            try {
                await Api.moderarProduto(botao.closest(".mod-item").dataset.id, botao.dataset.aprovar === "1");
                mostrarToast(botao.dataset.aprovar === "1" ? "Produto publicado." : "Produto rejeitado.");
                carregarProdutos();
            } catch (e) { mostrarToast(e.message, "erro"); botao.disabled = false; }
        });

        el("pendentes-pagamentos")?.addEventListener("click", async (ev) => {
            const botao = ev.target.closest("[data-confirmar]");
            if (!botao) return;
            botao.disabled = true;
            try { await Api.confirmarPagamento(botao.closest(".mod-item").dataset.ref); mostrarToast("Pagamento confirmado."); carregarPagamentos(); }
            catch (e) { mostrarToast(e.message, "erro"); botao.disabled = false; }
        });
    });
})();
