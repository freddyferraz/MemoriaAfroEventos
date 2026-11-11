/* Compra de ingressos na página do evento (exige conta; evento gratuito gera o ingresso na hora). */
(function () {
    const q = (id) => document.getElementById(id);

    document.addEventListener("evento-carregado", (ev) => {
        const e = ev.detail;
        const alvo = q("extra-ingressos");
        if (!alvo || e.data < EventosUI.iso(new Date())) return; // evento que já passou não vende ingresso

        const preco = e.gratuito ? "Entrada gratuita" : `${Loja.moeda(e.preco)} por ingresso`;
        if (!Session.logado()) {
            alvo.innerHTML = `<section class="extra"><h2>Ingressos</h2><p><strong>${preco}</strong></p>${Loja.convidarLogin("Para garantir o seu ingresso pela plataforma, é preciso ter uma conta.")}</section>`;
            return;
        }

        const opcoes = Array.from({ length: 10 }, (_, i) => `<option value="${i + 1}">${i + 1}</option>`).join("");
        alvo.innerHTML = `
            <section class="extra">
                <h2>Ingressos</h2>
                <p><strong>${preco}</strong></p>
                <form id="form-ingresso" class="compra-ingresso">
                    <div class="form-field" style="margin:0"><label for="ing-qtd">Quantidade</label><select id="ing-qtd">${opcoes}</select></div>
                    <div id="ing-total" style="font-weight:700"></div>
                    <button type="submit" class="btn btn-primary" id="ing-btn">${e.gratuito ? "Reservar ingresso gratuito" : "Comprar ingresso"}</button>
                </form>
                <p class="form-hint">Venda de ingressos em modo de demonstração: o pagamento é simulado e nenhum valor real é cobrado.</p>
                <p class="erro-form" id="ing-erro" role="alert"></p>
                <div id="ing-ok"></div>
            </section>`;

        const total = () => {
            const n = Number(q("ing-qtd").value);
            q("ing-total").textContent = e.gratuito ? "" : `Total: ${Loja.moeda(e.preco * n)}`;
        };
        total();
        q("ing-qtd").addEventListener("change", total);

        q("form-ingresso").addEventListener("submit", async (x) => {
            x.preventDefault();
            q("ing-erro").textContent = "";
            q("ing-btn").disabled = true;
            try {
                const r = await Api.comprarIngresso({ eventoId: e.id, quantidade: Number(q("ing-qtd").value) });
                if (r.gratuito) {
                    q("form-ingresso").hidden = true;
                    q("ing-ok").innerHTML = '<div class="msg-box"><h3>Ingresso reservado</h3><p>Seu código de entrada está em “Meus ingressos”.</p><a class="btn btn-primary" href="meus-ingressos.html">Ver meus ingressos</a></div>';
                } else {
                    location.href = r.urlPagamento;
                }
            } catch (err) {
                q("ing-erro").textContent = err.status === 429 ? "Muitas tentativas. Tente novamente mais tarde." : err.message;
                q("ing-btn").disabled = false;
            }
        });
    });
})();
