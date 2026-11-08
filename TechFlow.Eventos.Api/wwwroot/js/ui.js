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
    fotoUrl: (id) => `/api/eventos/${id}/foto`,
    card(e) {
        const { dia, mes } = this.diaMes(e.data);
        const dist = e.distanciaKm != null ? ` (${e.distanciaKm} km)` : "";
        return `
      <a class="ev-card" href="evento.html?id=${e.id}">
        <div class="ev-corpo">
          <div class="ev-data ${e.gratuito ? "gratis" : ""}"><strong>${dia}</strong><span>${mes}</span></div>
          ${e.temFoto ? `<img class="ev-mini" src="${this.fotoUrl(e.id)}" alt="Foto do evento ${escapeHtml(e.titulo)}" loading="lazy" />` : ""}
          <div class="ev-texto">
            <h3>${escapeHtml(e.titulo)}</h3>
            <span class="tag">${escapeHtml(e.categoriaNome)}</span>
            <span class="tag ${e.gratuito ? "gratis" : ""}">${this.preco(e)}</span>
            <p class="ev-meta">${this.hora(e.horario)}, ${escapeHtml(e.cidade)}${dist}<span class="nota" data-nota="${e.id}"></span></p>
          </div>
        </div>
      </a>`;
    },
    vazio: (msg) => `<div class="empty-state">${msg}</div>`,
};

/* Nota média nos cards (módulo Avaliações). Os cards nascem com um espaço vazio e este bloco o preenche,
   buscando o resumo de todos os eventos da tela em uma única chamada. */
(function () {
    const cache = new Map();
    let timer;

    function aplicar(span, r) {
        if (r && r.total > 0) {
            span.innerHTML = `<span class="estrela" aria-hidden="true">★</span> ${r.media.toFixed(1).replace(".", ",")} (${r.total})`;
            span.setAttribute("aria-label", `Nota ${r.media} de 5, ${r.total} ${r.total === 1 ? "avaliação" : "avaliações"}`);
        }
        span.dataset.ok = "1";
    }

    async function preencher() {
        const spans = [...document.querySelectorAll("[data-nota]:not([data-ok])")];
        if (!spans.length) return;
        const novos = [...new Set(spans.map((s) => Number(s.dataset.nota)).filter((id) => !cache.has(id)))];
        if (novos.length) {
            try {
                const lista = await Api.resumoAvaliacoes(novos);
                novos.forEach((id) => cache.set(id, null));
                lista.forEach((r) => cache.set(r.eventoId, r));
            } catch { return; }
        }
        spans.forEach((s) => aplicar(s, cache.get(Number(s.dataset.nota))));
    }

    document.addEventListener("DOMContentLoaded", () => {
        new MutationObserver(() => { clearTimeout(timer); timer = setTimeout(preencher, 60); })
            .observe(document.body, { childList: true, subtree: true });
        preencher();
    });
})();
