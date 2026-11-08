/* Avaliações e comentários na página do evento. */
(function () {
    const q = (id) => document.getElementById(id);
    const estrelas = (n) => "★".repeat(n) + "☆".repeat(5 - n);
    const media = (m) => m.toFixed(1).replace(".", ",");
    const data = (iso) => new Date(iso).toLocaleDateString("pt-BR");

    function lista(av) {
        if (!av.length) return '<p class="ev-meta">Ainda não há comentários publicados.</p>';
        return av.map((a) => `
            <div class="avaliacao">
                <span class="quem">${escapeHtml(a.nome)}</span>
                <span class="estrela" role="img" aria-label="Nota ${a.nota} de 5">${estrelas(a.nota)}</span>
                <span class="quando">${data(a.dataEnvio)}</span>
                ${a.comentario ? `<p>${escapeHtml(a.comentario)}</p>` : ""}
            </div>`).join("");
    }

    function formulario() {
        const opcoes = [5, 4, 3, 2, 1].map((n) =>
            `<label class="nota-op"><input type="radio" name="nota" value="${n}" /> ${n} <span class="estrela" aria-hidden="true">★</span></label>`).join("");
        return `
            <form id="form-avaliacao" novalidate>
                <h3>Conte como foi</h3>
                <fieldset class="estrelas"><legend>Sua nota *</legend>${opcoes}</fieldset>
                <div class="form-field"><label for="av-nome">Seu nome (opcional)</label><input type="text" id="av-nome" maxlength="60" /></div>
                <div class="form-field"><label for="av-comentario">Comentário (opcional)</label>
                    <textarea id="av-comentario" maxlength="1000" rows="3"></textarea>
                    <div class="form-hint">Não inclua links. A nota aparece na hora; comentários passam por revisão antes de serem publicados.</div></div>
                <div class="hp" aria-hidden="true"><label>Não preencha <input type="text" id="av-website" tabindex="-1" autocomplete="off" /></label></div>
                <p class="erro-form" id="av-erro" role="alert"></p>
                <button type="submit" class="btn btn-primary btn-small" id="av-enviar">Enviar avaliação</button>
            </form>`;
    }

    async function carregar(e) {
        const alvo = q("extra-avaliacoes");
        if (!alvo) return;
        try {
            const r = await Api.avaliacoesDoEvento(e.id);
            const resumo = r.total > 0
                ? `<p class="resumo-nota"><span class="estrela" aria-hidden="true">★</span> ${media(r.media)} de 5 (${r.total} ${r.total === 1 ? "avaliação" : "avaliações"})</p>` : "";
            alvo.innerHTML = `
                <section class="extra">
                    <h2>Avaliações</h2>
                    ${resumo}
                    <div id="av-lista">${lista(r.avaliacoes)}</div>
                    ${r.jaAconteceu ? formulario() : '<p class="form-hint" style="margin-top:14px">Você poderá avaliar este evento depois que ele acontecer.</p>'}
                </section>`;
            if (r.jaAconteceu) ligarFormulario(e);
        } catch { /* a seção não aparece se o módulo estiver indisponível */ }
    }

    function ligarFormulario(e) {
        q("form-avaliacao").addEventListener("submit", async (ev) => {
            ev.preventDefault();
            const erro = q("av-erro");
            erro.textContent = "";
            const nota = Number(document.querySelector('input[name="nota"]:checked')?.value || 0);
            if (!nota) { erro.textContent = "Escolha uma nota de 1 a 5."; return; }

            const botao = q("av-enviar");
            botao.disabled = true;
            try {
                const r = await Api.enviarAvaliacao({
                    eventoId: e.id, nota,
                    nome: q("av-nome").value.trim(),
                    comentario: q("av-comentario").value.trim(),
                    website: q("av-website").value,
                });
                if (nota >= 4) Interesses?.registrar(e, 2);
                mostrarToast(r.mensagem);
                carregar(e);
            } catch (x) {
                erro.textContent = x.status === 429 ? "Muitas avaliações deste dispositivo. Tente mais tarde." : x.message;
                botao.disabled = false;
            }
        });
    }

    document.addEventListener("evento-carregado", (ev) => carregar(ev.detail));
})();
