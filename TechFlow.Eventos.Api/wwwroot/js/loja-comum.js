/* Funções compartilhadas pelas páginas da loja e dos planos */
const Loja = {
    moeda: (v) => Number(v).toLocaleString("pt-BR", { style: "currency", currency: "BRL" }),
    fotoUrl: (id) => `/api/produtos/${id}/foto`,
    statusPedido: { 1: "Aguardando pagamento", 2: "Pago", 3: "Enviado", 4: "Entregue", 5: "Cancelado" },
    statusProduto: { 1: "Em análise", 2: "Publicado", 3: "Rejeitado", 4: "Pausado" },
    data: (iso) => new Date(iso).toLocaleDateString("pt-BR"),

    cartao(p) {
        const foto = p.temFoto
            ? `<img class="prod-foto" src="${Loja.fotoUrl(p.id)}" alt="Foto de ${escapeHtml(p.titulo)}" loading="lazy" />`
            : '<div class="prod-foto prod-sem-foto" aria-hidden="true">Sem foto</div>';
        return `
            <a class="prod-card" href="produto.html?id=${p.id}">
                ${foto}
                <div class="prod-info">
                    <h3>${escapeHtml(p.titulo)}</h3>
                    <p class="prod-preco">${Loja.moeda(p.preco)}</p>
                    <p class="ev-meta">${escapeHtml(p.vendedorNome)}, ${escapeHtml(p.cidade)}</p>
                    ${p.disponivel ? "" : '<span class="tag">Esgotado</span>'}
                </div>
            </a>`;
    },

    /** Reduz a foto (lado maior de 1280 px) e converte para JPEG antes do envio (limite do servidor: 2 MB). */
    async prepararFoto(arquivo) {
        if (!arquivo) return null;
        if (!/^image\/(jpeg|png|webp)$/.test(arquivo.type)) throw new Error("Escolha uma foto JPEG, PNG ou WebP.");
        const MAX = 2 * 1024 * 1024;
        if (!window.createImageBitmap) {
            if (arquivo.size > MAX) throw new Error("A foto passa de 2 MB. Escolha uma imagem menor.");
            return arquivo;
        }
        const bmp = await createImageBitmap(arquivo);
        const escala = Math.min(1, 1280 / Math.max(bmp.width, bmp.height));
        const canvas = document.createElement("canvas");
        canvas.width = Math.round(bmp.width * escala);
        canvas.height = Math.round(bmp.height * escala);
        const ctx = canvas.getContext("2d");
        ctx.fillStyle = "#fff";
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.drawImage(bmp, 0, 0, canvas.width, canvas.height);
        const blob = await new Promise((ok) => canvas.toBlob(ok, "image/jpeg", 0.85));
        if (!blob || blob.size > MAX) throw new Error("A foto passa de 2 MB mesmo depois de reduzida. Escolha outra.");
        return blob;
    },

    convidarLogin(texto) {
        return `<div class="aviso">${texto} <a href="login.html">Entrar</a> ou <a href="cadastro.html">criar conta</a>.</div>`;
    },
};
