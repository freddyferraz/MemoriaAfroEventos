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
  logado() { return !!this.obter(); },
  ehModerador() { return !!this.obter()?.roles?.some((r) => r === "Admin" || r === "Moderador"); },
};

async function apiRequest(path, { method = "GET", body } = {}) {
  const headers = {};
  const auth = Session.obter();
  if (auth?.token) headers.Authorization = `Bearer ${auth.token}`;
  // FormData (upload de foto): o navegador define o Content-Type com o boundary.
  const ehForm = body instanceof FormData;
  if (body && !ehForm) headers["Content-Type"] = "application/json";

  const resp = await fetch(`${API_BASE}${path}`, { method, headers, body: body ? (ehForm ? body : JSON.stringify(body)) : undefined });
  if (resp.status === 204) return null;

  const json = (resp.headers.get("content-type") || "").includes("json") ? await resp.json() : null;
  if (!resp.ok) {
    let msg = "Erro inesperado.";
    if (Array.isArray(json)) msg = json.join(" ");
    else if (json?.erro) msg = json.erro;
    else if (json?.errors) msg = Object.values(json.errors).flat().join(" ");
    else if (resp.status === 413) msg = "O arquivo enviado é grande demais. Use uma foto menor.";
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
  assinarNewsletter: (dto) => apiRequest("/newsletter/assinar", { method: "POST", body: dto }),
  confirmarNewsletter: (token) => apiRequest("/newsletter/confirmar", { method: "POST", body: { token } }),
  cancelarNewsletter: (token) => apiRequest("/newsletter/cancelar", { method: "POST", body: { token } }),
  moderarEvento: (id, dto) => apiRequest(`/eventos/${id}/moderar`, { method: "PATCH", body: dto }),
  listarProdutos: (f = {}) => apiRequest(`/produtos${montarQuery(f)}`),
  obterProduto: (id) => apiRequest(`/produtos/${id}`),
  categoriasProduto: () => apiRequest("/produtos/categorias"),
  cidadesProduto: () => apiRequest("/produtos/cidades"),
  meusProdutos: () => apiRequest("/produtos/meus"),
  anunciarProduto: (formData) => apiRequest("/produtos", { method: "POST", body: formData }),
  atualizarProduto: (id, dto) => apiRequest(`/produtos/${id}`, { method: "PATCH", body: dto }),
  produtosPendentes: () => apiRequest("/produtos/pendentes"),
  moderarProduto: (id, aprovar) => apiRequest(`/produtos/${id}/moderar`, { method: "PATCH", body: { aprovar } }),
  comprar: (dto) => apiRequest("/pedidos", { method: "POST", body: dto }),
  minhasCompras: () => apiRequest("/pedidos/compras"),
  minhasVendas: () => apiRequest("/pedidos/vendas"),
  acaoPedido: (id, acao) => apiRequest(`/pedidos/${id}/${acao}`, { method: "PATCH" }),
  listarPlanos: () => apiRequest("/planos"),
  meuPlano: () => apiRequest("/planos/meu"),
  contratarPlano: (plano) => apiRequest("/planos/contratar", { method: "POST", body: { plano } }),
  obterPagamento: (ref) => apiRequest(`/pagamentos/${ref}`),
  comprarIngresso: (dto) => apiRequest("/ingressos", { method: "POST", body: dto }),
  meusIngressos: () => apiRequest("/ingressos/meus"),
  cancelarIngresso: (id) => apiRequest(`/ingressos/${id}/cancelar`, { method: "PATCH" }),
  pagarPagamento: (ref, dto) => apiRequest(`/pagamentos/${ref}/pagar`, { method: "POST", body: dto }),
  simularPagamento: (ref) => apiRequest(`/pagamentos/${ref}/simular`, { method: "POST" }),
  pagamentosPendentes: () => apiRequest("/pagamentos/pendentes"),
  confirmarPagamento: (ref) => apiRequest(`/pagamentos/${ref}/confirmar`, { method: "POST" }),
  registrar: (dto) => apiRequest("/auth/registrar", { method: "POST", body: dto }),
  listarCadastrosPendentes: () => apiRequest("/auth/cadastros/pendentes"),
  moderarCadastro: (id, aprovar) => apiRequest(`/auth/cadastros/${id}/moderar`, { method: "PATCH", body: { aprovar } }),
  meusInteresses: () => apiRequest("/recomendacoes/meus-interesses"),
  salvarInteresses: (dto) => apiRequest("/recomendacoes/meus-interesses", { method: "PUT", body: dto }),
  resumoAvaliacoes: (ids) => apiRequest(`/avaliacoes/resumo?ids=${ids.join(",")}`),
  avaliacoesDoEvento: (id) => apiRequest(`/avaliacoes/evento/${id}`),
  enviarAvaliacao: (dto) => apiRequest("/avaliacoes", { method: "POST", body: dto }),
  listarAvaliacoesPendentes: () => apiRequest("/avaliacoes/pendentes"),
  moderarAvaliacao: (id, aprovar) => apiRequest(`/avaliacoes/${id}/moderar`, { method: "PATCH", body: { aprovar } }),
  linksCalendario: (id) => apiRequest(`/calendario/evento/${id}/links`),
  recomendar: (dto) => apiRequest("/recomendacoes", { method: "POST", body: dto }),
  eventosSimilares: (id, limite = 4) => apiRequest(`/recomendacoes/similares/${id}?limite=${limite}`),

  /** Foto de evento ainda não aprovado: só moderadores, então a busca leva o token e devolve uma URL local (blob). */
  async obterFotoBlob(id) {
    const auth = Session.obter();
    const resp = await fetch(`${API_BASE}/eventos/${id}/foto`, { headers: auth?.token ? { Authorization: `Bearer ${auth.token}` } : {} });
    if (!resp.ok) throw new Error("Foto indisponível.");
    return URL.createObjectURL(await resp.blob());
  },
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
    { href: "newsletter.html", label: "Newsletter" },
    { href: "loja.html", label: "Loja" },
    { href: "planos.html", label: "Planos" },
  ];
  if (auth) {
    paginas.push({ href: "meus-pedidos.html", label: "Meus pedidos" }, { href: "meus-produtos.html", label: "Meus produtos" }, { href: "meus-ingressos.html", label: "Meus ingressos" });
  }
  if (Session.ehModerador()) {
    paginas.push({ href: "moderar.html", label: "Moderar" }, { href: "qr.html", label: "QR codes" });
  }
  const links = paginas.map((p) => `<a href="${p.href}" ${p.href === paginaAtual ? 'aria-current="page"' : ""}>${p.label}</a>`).join("");
  const acesso = auth
    ? `<button class="btn btn-outline btn-small" id="btn-logout" type="button">Sair (${escapeHtml((auth.nomeCompleto || auth.email).split(" ")[0])})</button>`
    : `<a class="btn btn-outline btn-small" href="login.html">Entrar</a> <a class="btn btn-primary btn-small" href="cadastro.html">Criar conta</a>`;

  document.getElementById("site-header").innerHTML = `
    <div class="container">
      <a class="brand" href="index.html"><b>CONECTA</b><i> CULTURA</i></a>
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
