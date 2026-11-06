/* Divulgar evento: formulário público; o evento nasce pendente */
const v = (id) => document.getElementById(id).value.trim();
const FOTO_MAX_BYTES = 2 * 1024 * 1024;

/** Reduz a foto (lado maior de 1280 px) e converte para JPEG antes do envio. */
async function prepararFoto(arquivo) {
  if (!arquivo) return null;
  if (!/^image\/(jpeg|png|webp)$/.test(arquivo.type)) throw new Error("Escolha uma foto JPEG, PNG ou WebP.");
  if (!window.createImageBitmap) {
    if (arquivo.size > FOTO_MAX_BYTES) throw new Error("A foto passa de 2 MB. Escolha uma imagem menor.");
    return arquivo;
  }
  const bmp = await createImageBitmap(arquivo);
  const escala = Math.min(1, 1280 / Math.max(bmp.width, bmp.height));
  const canvas = document.createElement("canvas");
  canvas.width = Math.round(bmp.width * escala);
  canvas.height = Math.round(bmp.height * escala);
  const ctx = canvas.getContext("2d");
  ctx.fillStyle = "#fff"; // fundo branco para PNG/WebP com transparência
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  ctx.drawImage(bmp, 0, 0, canvas.width, canvas.height);
  const blob = await new Promise((ok) => canvas.toBlob(ok, "image/jpeg", 0.85));
  if (!blob) throw new Error("Não foi possível processar a foto.");
  if (blob.size > FOTO_MAX_BYTES) throw new Error("A foto passa de 2 MB mesmo depois de reduzida. Escolha outra.");
  return blob;
}

document.addEventListener("DOMContentLoaded", async () => {
  document.getElementById("e-data").min = EventosUI.iso(new Date());
  try {
    const [cats, cidades] = await Promise.all([Api.listarCategoriasEvento(), Api.listarCidadesEvento()]);
    document.getElementById("e-categoria").innerHTML = cats.map((c) => `<option value="${c.id}">${escapeHtml(c.nome)}</option>`).join("");
    document.getElementById("lista-cidades").innerHTML = cidades.map((c) => `<option value="${escapeHtml(c)}">`).join("");
  } catch {
    document.getElementById("erro").textContent = "Não foi possível carregar as categorias. Verifique se a API está no ar.";
  }

  document.getElementById("e-foto").addEventListener("change", (ev) => {
    const previa = document.getElementById("foto-previa");
    const arq = ev.target.files[0];
    if (previa.src.startsWith("blob:")) URL.revokeObjectURL(previa.src);
    if (arq) { previa.src = URL.createObjectURL(arq); previa.hidden = false; } else { previa.removeAttribute("src"); previa.hidden = true; }
  });

  document.getElementById("form-evento").addEventListener("submit", async (ev) => {
    ev.preventDefault();
    const erro = document.getElementById("erro");
    erro.textContent = "";
    const obrigatorios = ["e-titulo", "e-categoria", "e-cidade", "e-data", "e-horario", "e-endereco", "e-organizador", "e-contato"];
    if (obrigatorios.some((id) => !v(id))) { erro.textContent = "Preencha os campos marcados com asterisco."; return; }

    const botao = document.getElementById("btn-enviar");
    botao.disabled = true;
    try {
      const fd = new FormData();
      const campos = {
        TipoAutor: v("e-tipo"), Titulo: v("e-titulo"), Categoria: v("e-categoria"), Cidade: v("e-cidade"),
        Data: v("e-data"), Horario: v("e-horario"), Endereco: v("e-endereco"), LinkGoogleMaps: v("e-mapa"),
        Atracoes: v("e-atracoes"), Preco: v("e-preco") || "0", Ingressos: v("e-ingressos"),
        Acessibilidade: v("e-acessibilidade"), Organizador: v("e-organizador"),
        FonteOficial: v("e-fonte"), ContatoEnvio: v("e-contato"),
      };
      Object.entries(campos).forEach(([k, val]) => { if (val !== "") fd.append(k, val); });
      const foto = await prepararFoto(document.getElementById("e-foto").files[0]);
      if (foto) fd.append("Foto", foto, "foto.jpg");
      await Api.enviarEvento(fd);
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
