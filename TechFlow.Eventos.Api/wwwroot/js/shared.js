/* TechFlow Eventos — utilitários compartilhados. Front e API ficam no mesmo host (caminhos relativos). */
const API_BASE = "/api";

const Session = {
  KEY: "techflow.auth",
  salvar(auth) { localStorage.setItem(this.KEY, JSON.stringify(auth)); },
  obter() {
    try {
      const auth = JSON.parse(localStorage.getItem(this.KEY) || "null");
      if (auth?.expiraEm && new Date(auth.expiraEm) < new Date()) { this.limpar(); return null; }
      return auth;
    } catch { return null; }
  },
  limpar() { localStorage.removeItem(this.KEY); },
  ehModerador() { return !!this.obter()?.roles?.some((r) => r === "Admin" || r === "Moderador"); },
};

async function apiRequest(path, { method = "GET", body } = {}) {
  const headers = {};
  const auth = Session.obter();
  if (auth?.token) headers.Authorization = `Bearer ${auth.token}`;
  if (body) headers["Content-Type"] = "application/json";

  const resp = await fetch(`${API_BASE}${path}`, { method, headers, body: body ? JSON.stringify(body) : undefined });
  if (resp.status === 204) return null;

  const json = (resp.headers.get("content-type") || "").includes("json") ? await resp.json() : null;
  if (!resp.ok) {
    let msg = "Erro inesperado.";
    if (Array.isArray(json)) msg = json.join(" ");
    else if (json?.erro) msg = json.erro;
    else if (json?.errors) msg = Object.values(json.errors).flat().join(" ");
    else if (resp.status === 401) msg = "Sua sessão expirou. Entre novamente.";
    else if (resp.status === 403) msg = "Você não tem permissão para esta ação.";
    const erro = new Error(msg);
    erro.status = resp.status;
    throw erro;
  }
  return json;
}

/* Monta ?a=1&b=2 ignorando valores vazios/falsos */
function montarQuery(obj) {
  const partes = Object.entries(obj)
    .filter(([, v]) => v !== "" && v !== null && v !== undefined && v !== false)
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v === true ? "true" : v)}`);
  return partes.length ? `?${partes.join("&")}` : "";
}

const Api = {
  login: (dto) => apiRequest("/auth/login", { method: "POST", body: dto }),
  listarEventos: (filtros = {}) => apiRequest(`/eventos${montarQuery(filtros)}`),
  obterEvento: (id) => apiRequest(`/eventos/${id}`),
  listarCategoriasEvento: () => apiRequest("/eventos/categorias"),
  listarCidadesEvento: () => apiRequest("/eventos/cidades"),
  enviarEvento: (dto) => apiRequest("/eventos", { method: "POST", body: dto }),
  listarEventosPendentes: () => apiRequest("/eventos/pendentes"),
  moderarEvento: (id, dto) => apiRequest(`/eventos/${id}/moderar`, { method: "PATCH", body: dto }),
};

function escapeHtml(texto) {
  const div = document.createElement("div");
  div.textContent = texto ?? "";
  return div.innerHTML;
}

function mostrarToast(mensagem, tipo = "sucesso") {
  let t = document.querySelector(".toast");
  if (!t) { t = document.createElement("div"); t.className = "toast"; t.setAttribute("role", "status"); document.body.appendChild(t); }
  t.textContent = mensagem;
  t.classList.toggle("toast-error", tipo === "erro");
  t.classList.add("show");
  clearTimeout(t._timer);
  t._timer = setTimeout(() => t.classList.remove("show"), 3800);
}

function montarHeader(paginaAtual) {
  const auth = Session.obter();
  const paginas = [
    { href: "index.html", label: "Início" },
    { href: "explorar.html", label: "Explorar" },
    { href: "divulgar.html", label: "Divulgar evento" },
  ];
  if (Session.ehModerador()) {
    paginas.push({ href: "moderar.html", label: "Moderar" }, { href: "qr.html", label: "QR codes" });
  }
  const links = paginas.map((p) => `<a href="${p.href}" ${p.href === paginaAtual ? 'aria-current="page"' : ""}>${p.label}</a>`).join("");
  const acesso = auth
    ? `<button class="btn btn-outline btn-small" id="btn-logout" type="button">Sair (${escapeHtml((auth.nomeCompleto || auth.email).split(" ")[0])})</button>`
    : `<a class="btn btn-outline btn-small" href="login.html">Entrar</a>`;

  document.getElementById("site-header").innerHTML = `
    <div class="container">
      <a class="brand" href="index.html"><b>COONECTA</b><i> CULTURA</i></a>
      <nav class="main-nav" aria-label="Navegação principal">${links}</nav>
      <div>${acesso}</div>
    </div>`;
  document.getElementById("btn-logout")?.addEventListener("click", () => {
    Session.limpar();
    location.href = "index.html";
  });
}

function montarFooter() {
  document.getElementById("site-footer").innerHTML = `
    <div class="container">
      <strong>TechFlow Projetos</strong> — agenda cultural colaborativa. Piloto: Sacra Família do Tinguá e região, RJ.
    </div>`;
}

document.addEventListener("DOMContentLoaded", () => {
  montarHeader(document.body.dataset.pagina || "");
  montarFooter();
});
