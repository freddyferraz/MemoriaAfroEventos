/* Carrossel: trilho horizontal com rolagem por toque, setas e deslize automático lento (opcional).
   Uso: <div class="carrossel" data-carrossel [data-autoplay data-velocidade="30"]><div class="carrossel-trilho">...</div></div>
   data-velocidade = pixels por segundo (padrão 30; menor = mais devagar). */
(function () {
    const reduzido = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const comportamento = reduzido ? "auto" : "smooth";
    const VELOCIDADE_PADRAO = 30;   // px/s: um card de 340 px leva cerca de 12 s para passar
    const ESPERA_NO_FIM = 2500;     // ms parado no último card
    const DURACAO_VOLTA = 1800;     // ms da volta suave ao primeiro card
    const ESPERA_NO_INICIO = 1500;  // ms parado antes de recomeçar

    function criarBotao(classe, simbolo, rotulo) {
        const b = document.createElement("button");
        b.type = "button";
        b.className = `carrossel-btn ${classe}`;
        b.textContent = simbolo;
        b.setAttribute("aria-label", rotulo);
        return b;
    }

    function iniciar(raiz) {
        const trilho = raiz.querySelector(".carrossel-trilho");
        if (!trilho || raiz.dataset.pronto) return;
        raiz.dataset.pronto = "1";

        raiz.setAttribute("role", "region");
        raiz.setAttribute("aria-roledescription", "carrossel");
        trilho.tabIndex = 0; // permite rolar com as setas do teclado
        trilho.setAttribute("aria-label", raiz.getAttribute("aria-label") || "Lista de eventos");

        const anterior = criarBotao("anterior", "‹", "Mostrar eventos anteriores");
        const proximo = criarBotao("proximo", "›", "Mostrar próximos eventos");
        raiz.append(anterior, proximo);

        const passo = () => {
            const filho = trilho.firstElementChild;
            const gap = parseFloat(getComputedStyle(trilho).columnGap) || 16;
            return filho ? filho.getBoundingClientRect().width + gap : trilho.clientWidth;
        };
        const noFim = () => trilho.scrollLeft + trilho.clientWidth >= trilho.scrollWidth - 2;

        let pausaBtn = null;
        function atualizar() {
            const rolavel = trilho.scrollWidth > trilho.clientWidth + 2;
            anterior.hidden = proximo.hidden = !rolavel;
            if (pausaBtn) pausaBtn.hidden = !rolavel;
            anterior.disabled = trilho.scrollLeft <= 2;
            proximo.disabled = noFim();
        }

        anterior.addEventListener("click", () => trilho.scrollBy({ left: -passo(), behavior: comportamento }));
        proximo.addEventListener("click", () => trilho.scrollBy({ left: passo(), behavior: comportamento }));
        trilho.addEventListener("scroll", () => requestAnimationFrame(atualizar), { passive: true });
        new ResizeObserver(atualizar).observe(trilho);
        new MutationObserver(atualizar).observe(trilho, { childList: true });

        // Deslize automático lento e contínuo. Para ao passar o mouse, focar ou tocar, e quem prefere menos movimento não o recebe.
        if (raiz.hasAttribute("data-autoplay") && !reduzido) {
            const velocidade = Number(raiz.dataset.velocidade) || VELOCIDADE_PADRAO;
            let pausado = false, interagindo = false, retomar;

            pausaBtn = document.createElement("button");
            pausaBtn.type = "button";
            pausaBtn.className = "carrossel-pausa";
            pausaBtn.textContent = "Pausar";
            pausaBtn.setAttribute("aria-label", "Pausar o deslize automático dos eventos");
            pausaBtn.addEventListener("click", () => {
                pausado = !pausado;
                pausaBtn.textContent = pausado ? "Continuar" : "Pausar";
                pausaBtn.setAttribute("aria-label", pausado ? "Retomar o deslize automático dos eventos" : "Pausar o deslize automático dos eventos");
            });
            raiz.append(pausaBtn);

            ["mouseenter", "focusin"].forEach((e) => raiz.addEventListener(e, () => { interagindo = true; }));
            ["mouseleave", "focusout"].forEach((e) => raiz.addEventListener(e, () => { interagindo = false; }));
            raiz.addEventListener("touchstart", () => { interagindo = true; clearTimeout(retomar); }, { passive: true });
            raiz.addEventListener("touchend", () => { retomar = setTimeout(() => { interagindo = false; }, 8000); }, { passive: true });

            let pos = trilho.scrollLeft; // posição em número decimal (scrollLeft arredonda para pixels inteiros)
            let estado = "indo", espera = 0, volta = null, ultimo = 0, snapDesligado = false;

            const quadro = (t) => {
                const dt = ultimo ? Math.min(t - ultimo, 64) : 0;
                ultimo = t;
                const max = trilho.scrollWidth - trilho.clientWidth;
                const parado = pausado || interagindo || document.hidden || max <= 2;

                if (parado) {
                    pos = trilho.scrollLeft; // acompanha rolagens manuais para retomar de onde a pessoa parou
                    if (snapDesligado) { trilho.style.scrollSnapType = ""; snapDesligado = false; }
                } else {
                    // O encaixe (scroll-snap) brigaria com o deslize contínuo; fica desligado enquanto ele roda.
                    if (!snapDesligado) { trilho.style.scrollSnapType = "none"; snapDesligado = true; }

                    if (estado === "indo") {
                        pos = Math.min(pos + (velocidade * dt) / 1000, max);
                        trilho.scrollLeft = pos;
                        if (pos >= max - 0.5) { estado = "espera-fim"; espera = ESPERA_NO_FIM; }
                    } else if (estado === "espera-fim") {
                        espera -= dt;
                        if (espera <= 0) { estado = "volta"; volta = { de: pos, decorrido: 0 }; }
                    } else if (estado === "volta") {
                        volta.decorrido += dt;
                        const p = Math.min(volta.decorrido / DURACAO_VOLTA, 1);
                        const suave = p < 0.5 ? 2 * p * p : 1 - Math.pow(-2 * p + 2, 2) / 2;
                        pos = volta.de * (1 - suave);
                        trilho.scrollLeft = pos;
                        if (p >= 1) { estado = "espera-inicio"; espera = ESPERA_NO_INICIO; }
                    } else {
                        espera -= dt;
                        if (espera <= 0) estado = "indo";
                    }
                }
                requestAnimationFrame(quadro);
            };
            requestAnimationFrame(quadro);
        }

        atualizar();
    }

    document.addEventListener("DOMContentLoaded", () => document.querySelectorAll("[data-carrossel]").forEach(iniciar));
})();
