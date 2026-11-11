/* Meus ingressos: compras confirmadas (com QR) e aguardando pagamento */
(function () {
    const el = (id) => document.getElementById(id);

    function cartao(i) {
        const confirmado = i.status === 2;
        const quando = `${new Date(i.data + "T00:00:00").toLocaleDateString("pt-BR", { weekday: "long", day: "2-digit", month: "long" })}, às ${i.horario.slice(0, 5)}`;
        const acoes = confirmado ? "" : `
            <div class="mod-acoes" style="margin-top:8px">
                ${i.urlPagamento ? `<a class="btn btn-primary btn-small" href="${escapeHtml(i.urlPagamento)}">Pagar</a>` : ""}
                <button class="btn btn-outline btn-small" data-cancelar>Cancelar</button>
            </div>`;
        return `
            <div class="ingresso-card ${confirmado ? "" : "pendente"}" data-id="${i.id}">
                <div>
                    <h3 style="margin-bottom:4px"><a href="evento.html?id=${i.eventoId}">${escapeHtml(i.evento)}</a></h3>
                    <div class="ev-meta">${escapeHtml(quando)}</div>
                    <div class="ev-meta">${escapeHtml(i.local)}</div>
                    <div class="ev-meta">${i.quantidade} ${i.quantidade === 1 ? "ingresso" : "ingressos"}, ${i.total > 0 ? Loja.moeda(i.total) : "gratuito"}</div>
                    <span class="chip-status ${confirmado ? "" : "alerta"}">${confirmado ? "Confirmado" : "Aguardando pagamento"}</span>
                    ${acoes}
                </div>
                ${confirmado ? `<div class="ingresso-qr"><div data-qr="${escapeHtml(i.codigo)}"></div><small>${escapeHtml(i.codigo)}</small></div>` : ""}
            </div>`;
    }

    async function carregar() {
        const alvo = el("lista");
        if (!Session.logado()) { alvo.innerHTML = Loja.convidarLogin("Entre para ver os seus ingressos."); return; }
        try {
            const itens = await Api.meusIngressos();
            if (!itens.length) { alvo.innerHTML = EventosUI.vazio('Você ainda não tem ingressos. <a href="index.html">Veja os eventos</a>.'); return; }
            alvo.innerHTML = itens.map(cartao).join("");
            alvo.querySelectorAll("[data-qr]").forEach((d) => {
                if (window.QRCode) new QRCode(d, { text: `conectacultura:ingresso:${d.dataset.qr}`, width: 130, height: 130, correctLevel: QRCode.CorrectLevel.M });
            });
        } catch (e) { alvo.innerHTML = EventosUI.vazio(escapeHtml(e.message)); }
    }

    document.addEventListener("DOMContentLoaded", () => {
        carregar();
        el("lista").addEventListener("click", async (ev) => {
            const botao = ev.target.closest("[data-cancelar]");
            if (!botao) return;
            botao.disabled = true;
            try { await Api.cancelarIngresso(botao.closest(".ingresso-card").dataset.id); mostrarToast("Compra cancelada."); carregar(); }
            catch (e) { mostrarToast(e.message, "erro"); botao.disabled = false; }
        });
    });
})();
