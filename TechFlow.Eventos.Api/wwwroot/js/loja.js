/* Loja: listagem pública com filtros */
(function () {
    const el = (id) => document.getElementById(id);

    async function buscar() {
        try {
            const itens = await Api.listarProdutos({
                q: el("f-q").value.trim(), categoria: el("f-categoria").value, cidade: el("f-cidade").value,
                precoMax: el("f-preco").value, ordem: el("f-ordem").value,
            });
            el("contagem").textContent = `${itens.length} ${itens.length === 1 ? "produto encontrado" : "produtos encontrados"}`;
            el("resultados").innerHTML = itens.length ? itens.map(Loja.cartao).join("")
                : EventosUI.vazio("Nenhum produto com esses filtros.");
        } catch {
            el("resultados").innerHTML = EventosUI.vazio("Não foi possível carregar a loja.");
        }
    }

    document.addEventListener("DOMContentLoaded", async () => {
        try {
            const [cats, cidades] = await Promise.all([Api.categoriasProduto(), Api.cidadesProduto()]);
            el("f-categoria").insertAdjacentHTML("beforeend", cats.map((c) => `<option value="${c.id}">${escapeHtml(c.nome)}</option>`).join(""));
            el("f-cidade").insertAdjacentHTML("beforeend", cidades.map((c) => `<option>${escapeHtml(c)}</option>`).join(""));
        } catch { /* filtros funcionam sem as listas */ }
        let timer;
        el("filtros").addEventListener("input", () => { clearTimeout(timer); timer = setTimeout(buscar, 250); });
        el("filtros").addEventListener("submit", (e) => e.preventDefault());
        buscar();
    });
})();
