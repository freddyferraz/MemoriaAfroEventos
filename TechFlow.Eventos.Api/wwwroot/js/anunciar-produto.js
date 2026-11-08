/* Anunciar produto (exige conta) */
(function () {
    const el = (id) => document.getElementById(id);

    document.addEventListener("DOMContentLoaded", async () => {
        if (!Session.logado()) { el("aviso-login").innerHTML = Loja.convidarLogin("Para anunciar um produto, é preciso ter uma conta."); return; }
        el("painel").hidden = false;

        try {
            const [cats, cidades] = await Promise.all([Api.categoriasProduto(), Api.cidadesProduto()]);
            el("p-categoria").innerHTML = cats.map((c) => `<option value="${c.id}">${escapeHtml(c.nome)}</option>`).join("");
            el("lista-cidades").innerHTML = cidades.map((c) => `<option value="${escapeHtml(c)}">`).join("");
        } catch { el("erro").textContent = "Não foi possível carregar as categorias."; }

        el("p-foto").addEventListener("change", (ev) => {
            const previa = el("foto-previa");
            const arq = ev.target.files[0];
            if (previa.src.startsWith("blob:")) URL.revokeObjectURL(previa.src);
            if (arq) { previa.src = URL.createObjectURL(arq); previa.hidden = false; } else { previa.removeAttribute("src"); previa.hidden = true; }
        });

        el("form-produto").addEventListener("submit", async (ev) => {
            ev.preventDefault();
            const erro = el("erro");
            erro.textContent = "";
            const v = (id) => el(id).value.trim();
            if (!v("p-titulo") || !v("p-cidade") || !v("p-preco") || !v("p-estoque")) { erro.textContent = "Preencha os campos marcados com asterisco."; return; }

            const botao = el("btn-enviar");
            botao.disabled = true;
            try {
                const fd = new FormData();
                fd.append("Titulo", v("p-titulo"));
                fd.append("Categoria", v("p-categoria"));
                fd.append("Cidade", v("p-cidade"));
                fd.append("Preco", v("p-preco"));
                fd.append("Estoque", v("p-estoque"));
                if (v("p-descricao")) fd.append("Descricao", v("p-descricao"));
                const foto = await Loja.prepararFoto(el("p-foto").files[0]);
                if (foto) fd.append("Foto", foto, "foto.jpg");

                const r = await Api.anunciarProduto(fd);
                el("painel").hidden = true;
                el("resultado").innerHTML = `
                    <div class="msg-box"><h2>Anúncio enviado</h2><p>${escapeHtml(r.mensagem)}</p>
                    <a class="btn btn-primary" href="meus-produtos.html">Ver meus produtos</a></div>`;
            } catch (e) {
                erro.textContent = e.status === 429 ? "Muitos envios deste dispositivo. Tente novamente mais tarde." : e.message;
                botao.disabled = false;
            }
        });
    });
})();
