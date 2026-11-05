document.addEventListener("DOMContentLoaded", () => {
  document.getElementById("form-login").addEventListener("submit", async (ev) => {
    ev.preventDefault();
    const erro = document.getElementById("erro");
    const botao = document.getElementById("btn-entrar");
    erro.textContent = "";
    botao.disabled = true;
    try {
      const auth = await Api.login({
        email: document.getElementById("l-email").value.trim(),
        senha: document.getElementById("l-senha").value,
      });
      Session.salvar(auth);
      location.href = auth.roles.some((r) => r === "Admin" || r === "Moderador") ? "moderar.html" : "index.html";
    } catch (e) {
      erro.textContent = e.message;
      botao.disabled = false;
    }
  });
});
