/* Página inicial da agenda: destaques, próximos eventos e programação semanal/mensal */
async function carregarInicio() {
  try {
    const [destaques, proximos] = await Promise.all([
      Api.listarEventos({ destaque: true }),
      Api.listarEventos(),
    ]);
    document.getElementById("destaques").innerHTML = destaques.length
      ? destaques.map((e) => EventosUI.card(e)).join("")
      : EventosUI.vazio("Nenhum evento em destaque no momento.");
    document.getElementById("proximos").innerHTML = proximos.length
      ? proximos.slice(0, 6).map((e) => EventosUI.card(e)).join("")
      : EventosUI.vazio("Ainda não há eventos aprovados. <a href=\"divulgar.html\">Divulgue o primeiro.</a>");
  } catch {
    document.getElementById("destaques").innerHTML = EventosUI.vazio("Não foi possível carregar os eventos. Verifique se a API está no ar.");
  }
}

async function carregarProgramacao(dias) {
  const alvo = document.getElementById("programacao");
  try {
    const eventos = await Api.listarEventos({ ate: EventosUI.somarDias(dias) });
    if (!eventos.length) {
      alvo.innerHTML = EventosUI.vazio("Nenhum evento neste período.");
      return;
    }
    const porDia = {};
    eventos.forEach((e) => (porDia[e.data] ||= []).push(e));
    alvo.innerHTML = Object.keys(porDia).sort().map((d) =>
      `<div class="dia-titulo">${escapeHtml(EventosUI.dataLonga(d))}</div>
       <div class="ev-grid">${porDia[d].map((e) => EventosUI.card(e)).join("")}</div>`).join("");
  } catch {
    alvo.innerHTML = EventosUI.vazio("Não foi possível carregar a programação.");
  }
}

document.addEventListener("DOMContentLoaded", () => {
  carregarInicio();
  carregarProgramacao(6);
  document.querySelectorAll(".tabs button").forEach((b) =>
    b.addEventListener("click", () => {
      document.querySelectorAll(".tabs button").forEach((x) => x.setAttribute("aria-pressed", x === b));
      carregarProgramacao(Number(b.dataset.dias));
    }));
});
