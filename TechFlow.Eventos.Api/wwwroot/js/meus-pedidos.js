/* Meus pedidos: compras e vendas */
(function () {
    const el = (id) => document.getElementById(id);
    let aba = "compras";

    const linhaBase = (p) => `
        <h3 style="margin-bottom:4px">Pedido #${p.id}: ${escapeHtml(p.titulo)}</h3>
        <div class="ev-meta">${p.quantidade} x ${Loja.moeda(p.precoUnitario)}, total ${Loja.moeda(p.total)} em ${Loja.data(p.criadoEm)}</div>
        <span class="chip-status ${p.status === 1 ? "alerta" : p.status === 5 ? "ruim" : ""}">${Loja.statusPedido[p.status]}</span>`;

    function compra(p) {
        const acoes = [];
        if (p.status === 1 && p.urlPagamento) acoes.push(`<a class="btn btn-primary btn-small" href="${escapeHtml(p.urlPagamento)}">Pagar</a>`);
        if (p.status === 1) acoes.push('<button class="btn btn-outline btn-small" data-acao="cancelar">Cancelar</button>');
        if (p.status === 3) acoes.push('<button class="btn btn-primary btn-small" data-acao="receber">Recebi o produto</button>');
        return `<div class="mod-item" data-id="${p.id}"><div>${linhaBase(p)}<div class="ev-meta">Entrega: ${escapeHtml(p.enderecoEntrega)}</div></div><div class="mod-acoes">${acoes.join("")}</div></div>`;
    }

    function venda(p) {
        const dados = p.enderecoEntrega
            ? `<div class="ev-meta">Comprador: ${escapeHtml(p.compradorNome)}, telefone ${escapeHtml(p.telefone)}</div><div class="ev-meta">Entregar em: ${escapeHtml(p.enderecoEntrega)}</div>`
            : `<div class="ev-meta">Comprador: ${escapeHtml(p.compradorNome)}. Os dados de entrega aparecem depois do pagamento.</div>`;
        const acao = p.status === 2 ? '<button class="btn btn-primary btn-small" data-acao="enviar">Marcar como enviado</button>' : "";
        return `<div class="mod-item" data-id="${p.id}"><div>${linhaBase(p)}${dados}</div><div class="mod-acoes">${acao}</div></div>`;
    }

    async function carregar() {
        const alvo = el("lista");
        if (!Session.logado()) { alvo.innerHTML = Loja.convidarLogin("Entre para ver seus pedidos."); return; }
        try {
            const itens = aba === "compras" ? await Api.minhasCompras() : await Api.minhasVendas();
            alvo.innerHTML = itens.length ? itens.map(aba === "compras" ? compra : venda).join("")
                : EventosUI.vazio(aba === "compras" ? "Você ainda não fez nenhuma compra." : "Você ainda não tem vendas.");
        } catch (e) { alvo.innerHTML = EventosUI.vazio(escapeHtml(e.message)); }
    }

    document.addEventListener("DOMContentLoaded", () => {
        carregar();
        document.querySelectorAll("[data-aba]").forEach((b) => b.addEventListener("click", () => {
            aba = b.dataset.aba;
            document.querySelectorAll("[data-aba]").forEach((x) => x.setAttribute("aria-pressed", x === b));
            carregar();
        }));
        el("lista").addEventListener("click", async (ev) => {
            const botao = ev.target.closest("[data-acao]");
            if (!botao) return;
            botao.disabled = true;
            try { await Api.acaoPedido(botao.closest(".mod-item").dataset.id, botao.dataset.acao); mostrarToast("Pedido atualizado."); carregar(); }
            catch (e) { mostrarToast(e.message, "erro"); botao.disabled = false; }
        });
    });
})();
