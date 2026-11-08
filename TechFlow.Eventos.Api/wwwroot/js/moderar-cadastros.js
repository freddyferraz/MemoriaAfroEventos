/* Moderação de cadastros de contas (Admin/Moderador). */
(function () {
    async function carregar() {
        const alvo = document.getElementById("pendentes-cadastros");
        if (!alvo || !Session.obter()) return;
        try {
            const lista = await Api.listarCadastrosPendentes();
            if (!lista.length) { alvo.innerHTML = EventosUI.vazio("Nenhum cadastro aguardando aprovação."); return; }
            alvo.innerHTML = lista.map((c) => `
                <div class="mod-item" data-id="${escapeHtml(c.id)}">
                    <div>
                        <h3 style="margin-bottom:4px">${escapeHtml(c.nomeCompleto)}</h3>
                        <div class="ev-meta">${escapeHtml(c.email)}${c.dataCadastro ? `, solicitado em ${new Date(c.dataCadastro).toLocaleDateString("pt-BR")}` : ""}</div>
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
        document.getElementById("pendentes-cadastros")?.addEventListener("click", async (ev) => {
            const botao = ev.target.closest("[data-aprovar]");
            if (!botao) return;
            botao.disabled = true;
            try {
                await Api.moderarCadastro(botao.closest(".mod-item").dataset.id, botao.dataset.aprovar === "1");
                mostrarToast(botao.dataset.aprovar === "1" ? "Cadastro aprovado." : "Cadastro rejeitado.");
                carregar();
            } catch (e) {
                mostrarToast(e.message, "erro");
                botao.disabled = false;
            }
        });
    });
})();
