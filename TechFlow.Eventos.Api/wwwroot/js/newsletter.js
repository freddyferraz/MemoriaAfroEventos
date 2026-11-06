/* Newsletter: inscrição, confirmação (?confirmar=token) e cancelamento (?cancelar=token) */
const el = (id) => document.getElementById(id);

function mostrarMensagem(texto, erro = false) {
  el("resultado").innerHTML = `<div class="msg-box ${erro ? "erro" : ""}"><p style="margin:0">${escapeHtml(texto)}</p></div>`;
}

async function confirmar(token) {
  el("form-newsletter").hidden = true;
  mostrarMensagem("Confirmando sua inscrição...");
  try {
    const r = await Api.confirmarNewsletter(token);
    mostrarMensagem(r.mensagem);
  } catch (e) {
    mostrarMensagem(e.message, true);
  }
}

function pedirCancelamento(token) {
  el("form-newsletter").hidden = true;
  el("resultado").innerHTML = `
    <div class="msg-box">
      <h2>Cancelar inscrição</h2>
      <p>Você deixará de receber os eventos por e-mail e seus dados serão removidos.</p>
      <button class="btn btn-primary" id="btn-cancelar" type="button">Confirmar cancelamento</button>
    </div>`;
  el("btn-cancelar").addEventListener("click", async () => {
    el("btn-cancelar").disabled = true;
    try {
      mostrarMensagem((await Api.cancelarNewsletter(token)).mensagem);
    } catch (e) {
      mostrarMensagem(e.message, true);
    }
  });
}

function marcados(container) {
  return [...el(container).querySelectorAll("input:checked")].map((i) => i.value);
}

async function carregarOpcoes() {
  try {
    const [cats, cidades] = await Promise.all([Api.listarCategoriasEvento(), Api.listarCidadesEvento()]);
    el("lista-categorias").innerHTML = cats
      .map((c) => `<label><input type="checkbox" value="${c.id}" /> ${escapeHtml(c.nome)}</label>`).join("");
    if (cidades.length) {
      el("lista-cidades").innerHTML = cidades
        .map((c) => `<label><input type="checkbox" value="${escapeHtml(c)}" /> ${escapeHtml(c)}</label>`).join("");
      el("bloco-cidades").hidden = false;
    }
  } catch {
    el("erro").textContent = "Não foi possível carregar as opções. Verifique se a API está no ar.";
  }
}

document.addEventListener("DOMContentLoaded", async () => {
  const q = new URLSearchParams(location.search);
  if (q.get("confirmar")) return confirmar(q.get("confirmar"));
  if (q.get("cancelar")) return pedirCancelamento(q.get("cancelar"));

  await carregarOpcoes();

  el("form-newsletter").addEventListener("submit", async (ev) => {
    ev.preventDefault();
    const erro = el("erro");
    erro.textContent = "";
    const email = el("n-email").value.trim();
    if (!email) { erro.textContent = "Informe seu e-mail."; return; }
    if (!el("n-aceite").checked) { erro.textContent = "Marque a caixa de concordância para se inscrever."; return; }

    const botao = el("btn-assinar");
    botao.disabled = true;
    try {
      const r = await Api.assinarNewsletter({
        email,
        categorias: marcados("lista-categorias").map(Number),
        cidades: marcados("lista-cidades"),
        aceiteLgpd: true,
      });
      el("form-newsletter").hidden = true;
      mostrarMensagem(r.mensagem);
    } catch (e) {
      erro.textContent = e.status === 429 ? "Muitos pedidos deste dispositivo. Tente novamente mais tarde." : e.message;
      botao.disabled = false;
    }
  });
});
