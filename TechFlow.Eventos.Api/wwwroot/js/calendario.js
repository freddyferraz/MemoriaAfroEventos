/* Integração com calendários: adicionar o evento à agenda pessoal e assinar a agenda por filtro. */
(function () {
    const q = (id) => document.getElementById(id);

    // ---------- Detalhes do evento ----------
    document.addEventListener("evento-carregado", async (ev) => {
        const e = ev.detail;
        const alvo = q("extra-calendario");
        if (!alvo || e.data < EventosUI.iso(new Date())) return; // evento que já passou não precisa ir para a agenda

        try {
            const l = await Api.linksCalendario(e.id);
            alvo.innerHTML = `
                <section class="extra">
                    <h2>Adicionar à sua agenda</h2>
                    <div class="cal-botoes">
                        <a class="btn btn-outline btn-small" href="${escapeHtml(l.google)}" target="_blank" rel="noopener">Google Agenda</a>
                        <a class="btn btn-outline btn-small" href="${escapeHtml(l.outlook)}" target="_blank" rel="noopener">Outlook.com</a>
                        <a class="btn btn-outline btn-small" href="${escapeHtml(l.microsoft365)}" target="_blank" rel="noopener">Microsoft 365</a>
                        <a class="btn btn-outline btn-small" href="${escapeHtml(l.ics)}" download>Apple e outros (.ics)</a>
                    </div>
                    <p class="form-hint">O evento não informa horário de término, então a agenda reserva 2 horas. O arquivo .ics inclui um lembrete 1 hora antes.</p>
                </section>`;
            alvo.querySelectorAll("a").forEach((a) => a.addEventListener("click", () => Interesses?.registrar(e, 2)));
        } catch { /* sem links, a seção não aparece */ }
    });

    // ---------- Explorar: assinar o filtro atual ----------
    function montarFeed() {
        const alvo = q("feed-calendario");
        if (!alvo) return;
        alvo.innerHTML = `
            <details class="feed">
                <summary>Assinar estes eventos no meu calendário</summary>
                <p class="form-hint">Seu calendário passa a mostrar os eventos novos sozinho, respeitando a categoria e a cidade escolhidas nos filtros.</p>
                <input id="feed-url" readonly aria-label="Endereço do calendário" />
                <button type="button" class="btn btn-outline btn-small" id="feed-copiar">Copiar link</button>
                <a class="btn btn-outline btn-small" id="feed-webcal" href="#">Abrir no aplicativo de calendário</a>
                <p class="form-hint">No Google Agenda: Outras agendas, Por URL, e cole o link copiado.</p>
            </details>`;

        const atualizar = () => {
            const f = typeof lerFiltros === "function" ? lerFiltros() : {};
            const url = new URL("/api/calendario/feed.ics", location.origin);
            if (f.categoria) url.searchParams.set("categoria", f.categoria);
            if (f.cidade) url.searchParams.set("cidade", f.cidade);
            q("feed-url").value = url.href;
            q("feed-webcal").href = url.href.replace(/^https?:/, "webcal:");
        };

        atualizar();
        q("filtros")?.addEventListener("input", atualizar);
        q("btn-limpar")?.addEventListener("click", () => setTimeout(atualizar, 0));
        q("feed-copiar").addEventListener("click", async () => {
            try { await navigator.clipboard.writeText(q("feed-url").value); mostrarToast("Link copiado."); }
            catch { q("feed-url").select(); }
        });
    }

    document.addEventListener("DOMContentLoaded", montarFeed);
})();
