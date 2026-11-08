/* Meus produtos: anúncios do vendedor */
(function () {
    const el = (id) => document.getElementById(id);
    const classe = { 1: "alerta", 2: "", 3: "ruim", 4: "alerta" };

    async function carregar() {
        const alvo = el("lista");
        if (!Session.logado()) { alvo.innerHTML = Loja.convidarLogin("Entre para ver seus produtos."); return; }
        try {
            const itens = await Api.meusProdutos();
            if (!itens.length) { alvo.innerHTML = EventosUI.vazio("Você ainda não anunciou nenhum produto."); return; }
            alvo.innerHTML = itens.map((p) => `
                <div class="mod-item" data-id="${p.id}" style="align-items:flex-start">
                    <div style="flex:1;min-width:240px">
                        <h3 style="margin-bottom:4px"><a href="produto.html?id=${p.id}">${escapeHtml(p.titulo)}</a></h3>
                        <span class="chip-status ${classe[p.status] || ""}">${Loja.statusProduto[p.status]}</span>
                        <div class="linha-edicao">
                            <div class="form-field"><label for="pr-${p.id}">Preço (R$)</label><input type="number" id="pr-${p.id}" min="1" step="0.01" value="${p.preco}" /></div>
                            <div class="form-field"><label for="es-${p.id}">Estoque</label><input type="number" id="es-${p.id}" min="0" max="10000" value="${p.estoque}" /></div>
                            <button class="btn btn-outline btn-small" data-salvar>Salvar</button>
                        </div>
                    </div>
                    <div class="mod-acoes">
                        ${p.status === 2 ? '<button class="btn btn-outline btn-small" data-ativo="0">Pausar</button>' : ""}
                        ${p.status === 4 ? '<button class="btn btn-primary btn-small" data-ativo="1">Reativar</button>' : ""}
                    </div>
                </div>`).join("");
        } catch (e) { alvo.innerHTML = EventosUI.vazio(escapeHtml(e.message)); }
    }

    document.addEventListener("DOMContentLoaded", () => {
        carregar();
        el("lista").addEventListener("click", async (ev) => {
            const item = ev.target.closest(".mod-item");
            const botao = ev.target.closest("button");
            if (!item || !botao) return;
            const id = item.dataset.id;
            const dto = {};
            if (botao.hasAttribute("data-salvar")) { dto.preco = Number(el(`pr-${id}`).value); dto.estoque = Number(el(`es-${id}`).value); }
            else if (botao.dataset.ativo !== undefined) dto.ativo = botao.dataset.ativo === "1";
            else return;
            botao.disabled = true;
            try { await Api.atualizarProduto(id, dto); mostrarToast("Anúncio atualizado."); carregar(); }
            catch (e) { mostrarToast(e.message, "erro"); botao.disabled = false; }
        });
    });
})();
