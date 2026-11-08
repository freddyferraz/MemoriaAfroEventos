/* Criar conta: o cadastro fica pendente até um moderador aprovar. */
(function () {
    const el = (id) => document.getElementById(id);

    document.addEventListener("DOMContentLoaded", () => {
        el("form-cadastro").addEventListener("submit", async (ev) => {
            ev.preventDefault();
            const erro = el("erro");
            erro.textContent = "";

            const nome = el("c-nome").value.trim();
            const email = el("c-email").value.trim();
            const senha = el("c-senha").value;
            if (!nome || !email || !senha) { erro.textContent = "Preencha todos os campos marcados com asterisco."; return; }
            if (senha !== el("c-senha2").value) { erro.textContent = "As senhas não conferem."; return; }
            if (!el("c-aceite").checked) { erro.textContent = "Marque a caixa de concordância para criar a conta."; return; }

            const botao = el("btn-cadastrar");
            botao.disabled = true;
            try {
                const r = await Api.registrar({ nomeCompleto: nome, email, senha, aceiteTermos: true });
                el("form-cadastro").hidden = true;
                el("resultado").innerHTML = `
                    <div class="msg-box">
                        <h2>Cadastro recebido</h2>
                        <p>${escapeHtml(r.mensagem)}</p>
                        <p>Depois da aprovação, entre com o e-mail e a senha que você cadastrou.</p>
                        <a class="btn btn-primary" href="index.html">Voltar ao início</a>
                    </div>`;
            } catch (e) {
                erro.textContent = e.status === 429 ? "Muitas tentativas deste dispositivo. Tente novamente mais tarde." : e.message;
                botao.disabled = false;
            }
        });
    });
})();
