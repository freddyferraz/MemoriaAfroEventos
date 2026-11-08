/* Detalhe do produto e compra (a compra exige conta) */
(function () {
    const el = (id) => document.getElementById(id);

    function painelCompra(p) {
        if (!p.disponivel) return '<div class="aviso"><strong>Produto esgotado.</strong></div>';
        if (!Session.logado()) return Loja.convidarLogin("Para comprar, é preciso ter uma conta.");
        return `
            <form id="form-compra" novalidate>
                <h3>Comprar</h3>
                <div class="form-field"><label for="c-qtd">Quantidade (disponível: ${p.estoque})</label>
                    <input type="number" id="c-qtd" min="1" max="${Math.min(p.estoque, 50)}" value="1" /></div>
                <div class="form-field"><label for="c-end">Endereço de entrega *</label>
                    <input type="text" id="c-end" maxlength="300" autocomplete="street-address" placeholder="Rua, número, bairro, cidade" /></div>
                <div class="form-field"><label for="c-tel">Telefone para contato *</label>
                    <input type="text" id="c-tel" maxlength="30" autocomplete="tel" /></div>
                <p class="form-hint">O vendedor só vê o seu endereço e telefone depois que o pagamento for confirmado.</p>
                <p class="erro-form" id="c-erro" role="alert"></p>
                <button type="submit" class="btn btn-primary btn-block" id="c-btn">Ir para o pagamento</button>
            </form>`;
    }

    function ligarCompra(p) {
        const form = el("form-compra");
        if (!form) return;
        form.addEventListener("submit", async (ev) => {
            ev.preventDefault();
            const erro = el("c-erro");
            erro.textContent = "";
            const dto = { produtoId: p.id, quantidade: Number(el("c-qtd").value) || 1, enderecoEntrega: el("c-end").value.trim(), telefone: el("c-tel").value.trim() };
            if (!dto.enderecoEntrega || !dto.telefone) { erro.textContent = "Informe o endereço de entrega e o telefone."; return; }
            el("c-btn").disabled = true;
            try {
                const r = await Api.comprar(dto);
                location.href = r.urlPagamento;
            } catch (e) {
                erro.textContent = e.status === 429 ? "Muitas tentativas. Tente novamente mais tarde." : e.message;
                el("c-btn").disabled = false;
            }
        });
    }

    document.addEventListener("DOMContentLoaded", async () => {
        const alvo = el("produto");
        const id = Number(new URLSearchParams(location.search).get("id"));
        if (!id) { alvo.innerHTML = EventosUI.vazio("Produto não informado."); return; }
        try {
            const p = await Api.obterProduto(id);
            document.title = `${p.titulo} — Conecta Cultura`;
            const foto = p.temFoto
                ? `<img class="prod-foto" src="${Loja.fotoUrl(p.id)}" alt="Foto de ${escapeHtml(p.titulo)}" />`
                : '<div class="prod-foto prod-sem-foto" aria-hidden="true">Sem foto</div>';
            alvo.innerHTML = `
                <div class="prod-detalhe">
                    <div>${foto}</div>
                    <div>
                        <span class="tag">${escapeHtml(p.categoriaNome)}</span>
                        <h1 style="margin-top:8px">${escapeHtml(p.titulo)}</h1>
                        <p class="prod-preco" style="font-size:1.8rem">${Loja.moeda(p.preco)}</p>
                        <p class="ev-meta">Vendido por ${escapeHtml(p.vendedorNome)}, ${escapeHtml(p.cidade)}</p>
                        <p style="white-space:pre-line">${escapeHtml(p.descricao) || "Sem descrição."}</p>
                        ${painelCompra(p)}
                    </div>
                </div>`;
            ligarCompra(p);
        } catch (e) {
            alvo.innerHTML = EventosUI.vazio(e.status === 404 ? "Produto não encontrado ou indisponível." : "Não foi possível carregar o produto.");
        }
    });
})();
