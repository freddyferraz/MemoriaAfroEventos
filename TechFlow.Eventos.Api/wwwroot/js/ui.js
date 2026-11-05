/* Agenda de eventos: funções de apresentação compartilhadas entre as páginas */
const EventosUI = {
  dataLocal: (iso) => new Date(`${iso}T00:00:00`),
  diaMes(iso) {
    const d = this.dataLocal(iso);
    return { dia: d.getDate(), mes: d.toLocaleDateString("pt-BR", { month: "short" }).replace(".", "") };
  },
  dataLonga: (iso) => EventosUI.dataLocal(iso).toLocaleDateString("pt-BR", { weekday: "long", day: "2-digit", month: "long" }),
  hora: (h) => h.slice(0, 5),
  preco: (e) => (e.gratuito ? "Gratuito" : `R$ ${e.preco.toFixed(2).replace(".", ",")}`),
  iso(d) {
    const m = String(d.getMonth() + 1).padStart(2, "0");
    return `${d.getFullYear()}-${m}-${String(d.getDate()).padStart(2, "0")}`;
  },
  somarDias(dias) {
    const d = new Date();
    d.setDate(d.getDate() + dias);
    return this.iso(d);
  },
  /** Transforma texto em link apenas quando começa com http(s). */
  linkOuTexto(txt) {
    if (!txt) return "Não informado";
    return /^https?:\/\//i.test(txt)
      ? `<a href="${escapeHtml(txt)}" target="_blank" rel="noopener">${escapeHtml(txt)}</a>`
      : escapeHtml(txt);
  },
  card(e) {
    const { dia, mes } = this.diaMes(e.data);
    const dist = e.distanciaKm != null ? ` (${e.distanciaKm} km)` : "";
    return `
      <a class="ev-card" href="evento.html?id=${e.id}">
        <div class="ev-data ${e.gratuito ? "gratis" : ""}"><strong>${dia}</strong><span>${mes}</span></div>
        <div>
          <h3>${escapeHtml(e.titulo)}</h3>
          <span class="tag">${escapeHtml(e.categoriaNome)}</span>
          <span class="tag ${e.gratuito ? "gratis" : ""}">${this.preco(e)}</span>
          <p class="ev-meta">${this.hora(e.horario)}, ${escapeHtml(e.cidade)}${dist}</p>
        </div>
      </a>`;
  },
  vazio: (msg) => `<div class="empty-state">${msg}</div>`,
};
