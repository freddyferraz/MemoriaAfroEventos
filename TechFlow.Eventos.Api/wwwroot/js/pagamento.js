/* Tela de pagamento. Modo Simulado: Pix, boleto, cartão de crédito e de débito, tudo FICTÍCIO (apresentação).
   Modo Manual: instruções de pagamento por fora, com confirmação por um administrador. */
(function () {
    const el = (id) => document.getElementById(id);
    const ref = new URLSearchParams(location.search).get("ref");
    let estado = { metodo: 1, escolherOutra: false };

    const NOME_METODO = { 1: "Pix", 2: "Boleto bancário", 3: "Cartão de crédito", 4: "Cartão de débito" };
    const DESTINO = { 1: ["meus-pedidos.html", "Ver meus pedidos"], 2: ["planos.html", "Ver meu plano"], 3: ["meus-ingressos.html", "Ver meus ingressos"] };

    const hora = (iso) => new Date(iso).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });
    const dia = (iso) => new Date(iso).toLocaleDateString("pt-BR");
    const so = (s) => s.replace(/\D/g, "");

    function descreverMetodo(p) {
        if (!p.metodo) return "";
        const base = NOME_METODO[p.metodo];
        if (p.metodo === 3) return `${base} em ${p.parcelas}x${p.finalCartao ? `, final ${p.finalCartao}` : ""}`;
        if (p.metodo === 4) return `${base}${p.finalCartao ? `, final ${p.finalCartao}` : ""}`;
        return base;
    }

    // ---------- validações do cartão (feitas no navegador; o número não é enviado) ----------
    function luhn(numero) {
        let soma = 0, dobrar = false;
        for (let i = numero.length - 1; i >= 0; i--) {
            let d = Number(numero[i]);
            if (dobrar) { d *= 2; if (d > 9) d -= 9; }
            soma += d;
            dobrar = !dobrar;
        }
        return numero.length >= 13 && soma % 10 === 0;
    }
    function validade(texto) {
        const m = /^(\d{2})\/(\d{2})$/.exec(texto);
        if (!m) return false;
        const mes = Number(m[1]), ano = 2000 + Number(m[2]);
        if (mes < 1 || mes > 12) return false;
        return new Date(ano, mes, 1) > new Date(); // vale até o fim do mês de validade
    }

    // ---------- código de barras fictício do boleto ----------
    function barras(digitos) {
        let x = 0, svg = "";
        for (let i = 0; i < digitos.length; i++) {
            const d = Number(digitos[i]);
            const larg = (d % 3) + 1;
            if (i % 2 === 0) svg += `<rect x="${x}" y="0" width="${larg}" height="60" fill="#000"/>`;
            x += larg + (d % 2);
        }
        return `<svg class="barcode" viewBox="0 0 ${x} 60" preserveAspectRatio="none" role="img" aria-label="Código de barras fictício do boleto">${svg}</svg>`;
    }

    const avisoDemo = '<div class="aviso"><strong>Ambiente de demonstração.</strong> Use apenas dados fictícios: nenhum valor é cobrado e o número do cartão não é enviado ao servidor (só os 4 últimos dígitos).</div>';

    // ---------- telas ----------
    function recibo(p) {
        const [href, rotulo] = DESTINO[p.tipo] || ["index.html", "Voltar ao início"];
        el("conteudo").innerHTML = `
            <div class="msg-box recibo">
                <h2>Pagamento confirmado</h2>
                <dl>
                    <dt>Referente a</dt><dd>${escapeHtml(p.descricao)}</dd>
                    <dt>Valor</dt><dd>${Loja.moeda(p.valor)}</dd>
                    ${p.metodo ? `<dt>Forma de pagamento</dt><dd>${escapeHtml(descreverMetodo(p))}</dd>` : ""}
                    ${p.pagoEm ? `<dt>Data</dt><dd>${dia(p.pagoEm)} às ${hora(p.pagoEm)}</dd>` : ""}
                    <dt>Referência</dt><dd>${escapeHtml(p.referencia)}</dd>
                </dl>
                <a class="btn btn-primary" href="${href}">${rotulo}</a>
            </div>`;
    }

    function formCartao(p, credito) {
        const parcelas = credito
            ? `<div class="form-field"><label for="k-parc">Parcelas</label><select id="k-parc">${
                p.opcoesParcelas.map((o) => `<option value="${o.parcelas}">${o.parcelas === 1 ? "À vista" : `${o.parcelas}x de ${Loja.moeda(o.valorParcela)} sem juros`}</option>`).join("")}</select></div>`
            : "";
        return `
            <p class="teste-dados">Dados de teste:
                <button type="button" data-teste="ok">cartão aprovado</button> ou
                <button type="button" data-teste="recusa">cartão recusado</button></p>
            <div class="form-field"><label for="k-num">Número do cartão</label><input id="k-num" inputmode="numeric" autocomplete="off" maxlength="23" placeholder="0000 0000 0000 0000" /></div>
            <div class="form-field"><label for="k-nome">Nome impresso no cartão</label><input id="k-nome" autocomplete="off" maxlength="60" /></div>
            <div class="form-linha">
                <div class="form-field"><label for="k-val">Validade</label><input id="k-val" inputmode="numeric" autocomplete="off" maxlength="5" placeholder="MM/AA" /></div>
                <div class="form-field"><label for="k-cvv">Código de segurança</label><input id="k-cvv" inputmode="numeric" autocomplete="off" maxlength="4" placeholder="CVV" /></div>
            </div>
            ${parcelas}
            <button type="submit" class="btn btn-primary btn-block" id="btn-pagar">Pagar ${Loja.moeda(p.valor)}</button>`;
    }

    function painelMetodo(p) {
        if (estado.metodo === 1) return `<p>Geramos um código Pix fictício para você pagar (simulação).</p><button type="submit" class="btn btn-primary btn-block" id="btn-pagar">Gerar Pix de ${Loja.moeda(p.valor)}</button>`;
        if (estado.metodo === 2) return `<p>Geramos um boleto fictício com vencimento em 3 dias (simulação).</p><button type="submit" class="btn btn-primary btn-block" id="btn-pagar">Gerar boleto de ${Loja.moeda(p.valor)}</button>`;
        return formCartao(p, estado.metodo === 3);
    }

    function escolher(p) {
        const opcoes = [1, 2, 3, 4].map((id) =>
            `<label class="metodo-op"><input type="radio" name="metodo" value="${id}" ${estado.metodo === id ? "checked" : ""} /> ${NOME_METODO[id]}</label>`).join("");
        el("conteudo").innerHTML = `
            <div class="form-panel">
                <p style="margin:0 0 4px">${escapeHtml(p.descricao)}</p>
                <p class="prod-preco" style="font-size:2rem">${Loja.moeda(p.valor)}</p>
                ${avisoDemo}
                <form id="form-pag" novalidate>
                    <fieldset class="metodos"><legend>Forma de pagamento</legend>${opcoes}</fieldset>
                    <div id="painel-metodo">${painelMetodo(p)}</div>
                    <p class="erro-form" id="erro" role="alert"></p>
                </form>
            </div>`;

        document.querySelectorAll('input[name="metodo"]').forEach((r) => r.addEventListener("change", () => {
            estado.metodo = Number(r.value);
            el("painel-metodo").innerHTML = painelMetodo(p);
            el("erro").textContent = "";
            ligarCartao();
        }));
        ligarCartao();
        el("form-pag").addEventListener("submit", (ev) => { ev.preventDefault(); enviar(p); });
    }

    function ligarCartao() {
        const num = el("k-num"), val = el("k-val");
        if (!num) return;
        num.addEventListener("input", () => { num.value = so(num.value).slice(0, 19).replace(/(\d{4})(?=\d)/g, "$1 "); });
        val.addEventListener("input", () => { const d = so(val.value).slice(0, 4); val.value = d.length > 2 ? `${d.slice(0, 2)}/${d.slice(2)}` : d; });
        document.querySelectorAll("[data-teste]").forEach((b) => b.addEventListener("click", () => {
            num.value = b.dataset.teste === "ok" ? "4111 1111 1111 1111" : "4000 0000 0000 0002";
            el("k-nome").value = "CLIENTE DEMONSTRACAO";
            val.value = "12/34";
            el("k-cvv").value = "123";
        }));
    }

    async function enviar(p) {
        const erro = el("erro");
        erro.textContent = "";
        const dto = { metodo: estado.metodo };
        if (estado.metodo >= 3) {
            const numero = so(el("k-num").value);
            if (!luhn(numero)) { erro.textContent = "Número de cartão inválido."; return; }
            if (el("k-nome").value.trim().length < 3) { erro.textContent = "Informe o nome impresso no cartão."; return; }
            if (!validade(el("k-val").value)) { erro.textContent = "Validade inválida ou vencida."; return; }
            if (!/^\d{3,4}$/.test(el("k-cvv").value)) { erro.textContent = "Código de segurança inválido."; return; }
            dto.finalCartao = numero.slice(-4); // o número completo fica no navegador
            if (estado.metodo === 3) dto.parcelas = Number(el("k-parc").value);
        }
        const botao = el("btn-pagar");
        botao.disabled = true;
        try {
            await Api.pagarPagamento(ref, dto);
            estado.escolherOutra = false;
            await carregar();
        } catch (e) {
            erro.textContent = e.status === 429 ? "Muitas tentativas. Tente novamente em alguns minutos." : e.message;
            botao.disabled = false;
        }
    }

    function aguardando(p) {
        const pix = p.metodo === 1;
        const conteudo = pix
            ? `<div class="pix-box">
                    <p>Escaneie o QR code ou use o Pix copia e cola.</p>
                    <div class="pix-qr" id="pix-qr"></div>
                    <textarea class="codigo-copia" id="codigo" rows="4" readonly>${escapeHtml(p.codigoPix)}</textarea>
                    <p class="form-hint">Válido até ${hora(p.venceEm)} (simulação).</p>
               </div>`
            : `<div class="boleto-box">
                    <p>Pague em qualquer banco ou aplicativo (simulação).</p>
                    ${barras(so(p.codigoBoleto))}
                    <div class="linha-digitavel" id="codigo">${escapeHtml(p.codigoBoleto)}</div>
                    <p class="form-hint">Vencimento: ${dia(p.venceEm)}. A compensação real leva até 2 dias úteis; aqui você pode simulá-la.</p>
               </div>`;
        el("conteudo").innerHTML = `
            <div class="form-panel recibo">
                <p style="margin:0 0 4px">${escapeHtml(p.descricao)}</p>
                <p class="prod-preco" style="font-size:2rem">${Loja.moeda(p.valor)}</p>
                ${avisoDemo}
                <h3>${pix ? "Pague com Pix" : "Boleto gerado"}</h3>
                ${conteudo}
                <div class="acoes-pag">
                    <button class="btn btn-outline btn-small" id="btn-copiar">Copiar código</button>
                    <button class="btn btn-primary" id="btn-simular">${pix ? "Simular pagamento do Pix" : "Simular compensação do boleto"}</button>
                    <button class="btn btn-outline btn-small" id="btn-outra">Escolher outra forma de pagamento</button>
                </div>
                <p class="erro-form" id="erro" role="alert" style="text-align:center"></p>
            </div>`;

        if (pix && window.QRCode) new QRCode(el("pix-qr"), { text: p.codigoPix, width: 190, height: 190, correctLevel: QRCode.CorrectLevel.M });
        el("btn-copiar").addEventListener("click", async () => {
            const texto = pix ? p.codigoPix : so(p.codigoBoleto);
            try { await navigator.clipboard.writeText(texto); mostrarToast("Código copiado."); } catch { mostrarToast("Não foi possível copiar. Selecione o código manualmente.", "erro"); }
        });
        el("btn-simular").addEventListener("click", async (ev) => {
            ev.target.disabled = true;
            try { await Api.simularPagamento(ref); await carregar(); }
            catch (e) { el("erro").textContent = e.message; ev.target.disabled = false; }
        });
        el("btn-outra").addEventListener("click", () => { estado.escolherOutra = true; carregar(); });
    }

    function manual(p) {
        const instrucoes = p.instrucoes
            ? `<div class="aviso" style="white-space:pre-line"><strong>Como pagar</strong>\n${escapeHtml(p.instrucoes)}\nCitar a referência: ${escapeHtml(p.referencia)}</div>` : "";
        el("conteudo").innerHTML = `
            <div class="form-panel">
                <p style="margin:0 0 4px">${escapeHtml(p.descricao)}</p>
                <p class="prod-preco" style="font-size:2rem">${Loja.moeda(p.valor)}</p>
                ${instrucoes}
                <p class="form-hint">Assim que o pagamento for confirmado, a sua conta será atualizada.</p>
                <a class="btn btn-outline btn-block" href="">Atualizar situação</a>
            </div>`;
    }

    async function carregar() {
        const alvo = el("conteudo");
        if (!Session.logado()) { alvo.innerHTML = Loja.convidarLogin("Entre para ver este pagamento."); return; }
        if (!ref) { alvo.innerHTML = EventosUI.vazio("Pagamento não informado."); return; }
        try {
            const p = await Api.obterPagamento(ref);
            if (p.status === 2) return recibo(p);
            if (p.status === 3) {
                const [href, rotulo] = DESTINO[p.tipo] || ["index.html", "Voltar ao início"];
                alvo.innerHTML = `<div class="msg-box erro"><p>Este pagamento foi cancelado.</p><a class="btn btn-outline" href="${href}">${rotulo}</a></div>`;
                return;
            }
            if (!p.simulavel) return manual(p);
            if ((p.metodo === 1 || p.metodo === 2) && (p.codigoPix || p.codigoBoleto) && !estado.escolherOutra) return aguardando(p);
            escolher(p);
        } catch (e) { alvo.innerHTML = EventosUI.vazio(e.status === 404 ? "Pagamento não encontrado." : escapeHtml(e.message)); }
    }

    document.addEventListener("DOMContentLoaded", carregar);
})();
