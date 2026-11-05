/* Explorar eventos: busca com filtros (cidade, distância, data, categoria, preço, gratuidade) */
let posicao = null;
const $ = (id) => document.getElementById(id);

function lerFiltros() {
  return {
    q: $("f-q").value.trim(),
    cidade: $("f-cidade").value,
    categoria: $("f-categoria").value,
    de: $("f-de").value,
    ate: $("f-ate").value,
    precoMax: $("f-preco").value,
    gratuito: $("f-gratis").checked,
    ...(posicao ? { lat: posicao.lat, lng: posicao.lng, distanciaKm: $("f-dist").value } : {}),
  };
}

async function buscar() {
  try {
    const eventos = await Api.listarEventos(lerFiltros());
    $("contagem").textContent = `${eventos.length} ${eventos.length === 1 ? "evento encontrado" : "eventos encontrados"}`;
    $("resultados").innerHTML = eventos.length
      ? eventos.map((e) => EventosUI.card(e)).join("")
      : EventosUI.vazio("Nenhum evento com esses filtros. Aumente a distância ou limpe os filtros.");
  } catch {
    $("resultados").innerHTML = EventosUI.vazio("Não foi possível carregar os eventos. Verifique se a API está no ar.");
  }
}

document.addEventListener("DOMContentLoaded", async () => {
  try {
    const [cats, cidades] = await Promise.all([Api.listarCategoriasEvento(), Api.listarCidadesEvento()]);
    $("f-categoria").insertAdjacentHTML("beforeend", cats.map((c) => `<option value="${c.id}">${escapeHtml(c.nome)}</option>`).join(""));
    $("f-cidade").insertAdjacentHTML("beforeend", cidades.map((c) => `<option>${escapeHtml(c)}</option>`).join(""));
  } catch { /* filtros continuam funcionando sem as listas */ }

  let timer;
  $("filtros").addEventListener("input", () => {
    $("f-dist-val").textContent = $("f-dist").value;
    clearTimeout(timer);
    timer = setTimeout(buscar, 250);
  });
  $("filtros").addEventListener("submit", (ev) => ev.preventDefault());

  $("btn-local").addEventListener("click", () => {
    if (!navigator.geolocation) { $("local-status").textContent = "Seu navegador não oferece localização."; return; }
    navigator.geolocation.getCurrentPosition(
      (p) => { posicao = { lat: p.coords.latitude, lng: p.coords.longitude }; $("local-status").textContent = "Localização ativa."; buscar(); },
      () => { $("local-status").textContent = "Não foi possível obter sua localização. Libere a permissão do navegador."; }
    );
  });

  $("btn-limpar").addEventListener("click", () => {
    $("filtros").reset();
    posicao = null;
    $("f-dist-val").textContent = "30";
    $("local-status").textContent = "Sem localização, a distância não é aplicada.";
    buscar();
  });

  buscar();
});
