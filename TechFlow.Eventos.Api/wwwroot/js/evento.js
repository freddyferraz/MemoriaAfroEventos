/* Detalhes do evento */
document.addEventListener("DOMContentLoaded", async () => {
    const alvo = document.getElementById("detalhe");
    const id = Number(new URLSearchParams(location.search).get("id"));
    if (!id) { alvo.innerHTML = EventosUI.vazio("Evento não informado."); return; }
    try {
        const e = await Api.obterEvento(id);
        document.title = `${e.titulo} — TechFlow Eventos`;
        const mapa = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(e.latitude != null ? `${e.latitude},${e.longitude}` : `${e.endereco}, ${e.cidade}`)}`;
        alvo.innerHTML = `
      <article class="detalhe">
        <span class="tag">${escapeHtml(e.categoriaNome)}</span>
        <span class="tag ${e.gratuito ? "gratis" : ""}">${EventosUI.preco(e)}</span>
        <h1 style="margin-top:8px">${escapeHtml(e.titulo)}</h1>
        <dl>
          <dt>Data e horário</dt><dd>${escapeHtml(EventosUI.dataLonga(e.data))}, às ${EventosUI.hora(e.horario)}</dd>
          <dt>Endereço</dt><dd>${escapeHtml(e.endereco)}, ${escapeHtml(e.cidade)} <a href="${mapa}" target="_blank" rel="noopener">Ver no mapa</a></dd>
          <dt>Atrações</dt><dd>${escapeHtml(e.atracoes) || "Não informado"}</dd>
          <dt>Preço</dt><dd>${EventosUI.preco(e)}</dd>
          <dt>Acessibilidade</dt><dd>${escapeHtml(e.acessibilidade) || "Não informado"}</dd>
          <dt>Ingressos</dt><dd>${EventosUI.linkOuTexto(e.ingressos)}</dd>
          <dt>Organizador</dt><dd>${escapeHtml(e.organizador)}</dd>
          <dt>Fonte oficial</dt><dd>${EventosUI.linkOuTexto(e.fonteOficial)}</dd>
        </dl>
        ${e.temFoto ? `<img class="ev-foto-detalhe" src="${EventosUI.fotoUrl(e.id)}" alt="Foto do evento ${escapeHtml(e.titulo)}" />` : ""}
        <button class="btn btn-outline" id="btn-compartilhar" type="button">Compartilhar</button>
        <span class="form-hint" id="msg-compartilhar"></span>
      </article>`;
        document.getElementById("btn-compartilhar").addEventListener("click", async () => {
            const msg = document.getElementById("msg-compartilhar");
            try { await navigator.clipboard.writeText(location.href); msg.textContent = " Link copiado."; }
            catch { msg.textContent = " Copie o endereço do navegador."; }
        });
        // Os módulos (calendário, avaliações, recomendações) escutam este evento e preenchem as suas áreas.
        document.dispatchEvent(new CustomEvent("evento-carregado", { detail: e }));
    } catch (erro) {
        alvo.innerHTML = EventosUI.vazio(erro.status === 404 ? "Evento não encontrado ou ainda em análise." : "Não foi possível carregar o evento.");
    }
});
