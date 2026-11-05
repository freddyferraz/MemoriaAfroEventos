/* Divulgar evento: formulário público; o evento nasce pendente */
const v = (id) => document.getElementById(id).value.trim();

document.addEventListener("DOMContentLoaded", async () => {
  document.getElementById("e-data").min = EventosUI.iso(new Date());
  try {
    const [cats, cidades] = await Promise.all([Api.listarCategoriasEvento(), Api.listarCidadesEvento()]);
    document.getElementById("e-categoria").innerHTML = cats.map((c) => `<option value="${c.id}">${escapeHtml(c.nome)}</option>`).join("");
    document.getElementById("lista-cidades").innerHTML = cidades.map((c) => `<option value="${escapeHtml(c)}">`).join("");
  } catch {
    document.getElementById("erro").textContent = "Não foi possível carregar as categorias. Verifique se a API está no ar.";
  }

  document.getElementById("form-evento").addEventListener("submit", async (ev) => {
    ev.preventDefault();
    const erro = document.getElementById("erro");
    erro.textContent = "";
    const obrigatorios = ["e-titulo", "e-categoria", "e-cidade", "e-data", "e-horario", "e-endereco", "e-organizador", "e-contato"];
    if (obrigatorios.some((id) => !v(id))) { erro.textContent = "Preencha os campos marcados com asterisco."; return; }

    const botao = document.getElementById("btn-enviar");
    botao.disabled = true;
    try {
      await Api.enviarEvento({
        tipoAutor: Number(v("e-tipo")), titulo: v("e-titulo"), categoria: Number(v("e-categoria")),
        cidade: v("e-cidade"), data: v("e-data"), horario: `${v("e-horario")}:00`, endereco: v("e-endereco"),
        linkGoogleMaps: v("e-mapa") || null, atracoes: v("e-atracoes"), preco: Number(v("e-preco")) || 0,
        ingressos: v("e-ingressos"), acessibilidade: v("e-acessibilidade"),
        organizador: v("e-organizador"), fonteOficial: v("e-fonte"), contatoEnvio: v("e-contato"),
      });
      document.getElementById("painel").innerHTML = `
        <h2>Evento enviado</h2>
        <p>Recebemos “${escapeHtml(v("e-titulo") || "seu evento")}”. Um moderador vai conferir os dados antes de publicar.</p>
        <a class="btn btn-primary" href="divulgar.html">Divulgar outro evento</a>
        <a class="btn btn-outline" href="index.html">Ver a agenda</a>`;
    } catch (e) {
      erro.textContent = e.message;
      botao.disabled = false;
    }
  });
});
