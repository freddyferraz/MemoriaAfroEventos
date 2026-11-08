/* Moderação de comentários (Admin/Moderador). */
(function () {
    const estrelas = (n) => "★".repeat(n) + "☆".repeat(5 - n);

    async function carregar() {
        const alvo = document.getElementById("pendentes-avaliacoes");
        if (!alvo || !Session.obter()) return;
        try {
            const lista = await Api.listarAvaliacoesPendentes();
            if (!lista.length) { alvo.innerHTML = EventosUI.vazio("Nenhum comentário aguardando revisão."); return; }
            alvo.innerHTML = lista.map((a) => `
                <div class="mod-item" data-id="${a.id}">
                    <div>
                        <h3 style="margin-bottom:4px">${escapeHtml(a.eventoTitulo)}</h3>
                        <div class="ev-meta"><span class="estrela" role="img" aria-label="Nota ${a.nota} de 5">${estrelas(a.nota)}</span> por ${escapeHtml(a.nome)}</div>
                        <p style="margin:8px 0 0;white-space:pre-line">${escapeHtml(a.comentario)}</p>
                    </div>
                    <div class="mod-acoes">
                        <button class="btn btn-primary btn-small" data-aprovar="1">Aprovar</button>
                        <button class="btn btn-small btn-perigo" data-aprovar="0">Rejeitar</button>
                    </div>
                </div>`).join("");
        } catch (e) {
            alvo.innerHTML = EventosUI.vazio(e.status === 403 ? "Esta conta não tem permissão de moderador." : escapeHtml(e.message));
        }
    }

    document.addEventListener("DOMContentLoaded", () => {
        carregar();
        document.getElementById("pendentes-avaliacoes")?.addEventListener("click", async (ev) => {
            const botao = ev.target.closest("[data-aprovar]");
            if (!botao) return;
            botao.disabled = true;
            try {
                await Api.moderarAvaliacao(botao.closest(".mod-item").dataset.id, botao.dataset.aprovar === "1");
                mostrarToast(botao.dataset.aprovar === "1" ? "Comentário publicado." : "Comentário rejeitado.");
                carregar();
            } catch (e) {
                mostrarToast(e.message, "erro");
                botao.disabled = false;
            }
        });
    });
})();
