/* Moderação de eventos (Admin/Moderador) */
const STATUS = { aprovar: 2, rejeitar: 3 };

async function carregarPendentes() {
  const alvo = document.getElementById("pendentes");
  const auth = Session.obter();
  if (!auth) { alvo.innerHTML = EventosUI.vazio('Entre com uma conta de moderador. <a href="login.html">Entrar</a>'); return; }
  try {
    const lista = await Api.listarEventosPendentes();
    if (!lista.length) { alvo.innerHTML = EventosUI.vazio("Nada pendente. Novos envios aparecem aqui."); return; }
    alvo.innerHTML = lista.map((e) => `
      <div class="mod-item" data-id="${e.id}">
        <div>
          <h3 style="margin-bottom:4px">${escapeHtml(e.titulo)}</h3>
          <div class="ev-meta">${escapeHtml(EventosUI.dataLonga(e.data))}, ${EventosUI.hora(e.horario)}. ${escapeHtml(e.cidade)}, ${escapeHtml(e.endereco)}</div>
          <div class="ev-meta">${escapeHtml(e.categoriaNome)}. ${EventosUI.preco(e)}. Organizador: ${escapeHtml(e.organizador)}. Fonte: ${EventosUI.linkOuTexto(e.fonteOficial)}</div>
          <div class="ev-meta">Enviado por ${e.tipoAutor === 2 ? "organizador" : "morador"}. Contato: ${escapeHtml(e.contatoEnvio || "")}</div>
        </div>
        <div class="mod-acoes">
          <label class="check" style="padding:0"><input type="checkbox" data-destaque /> Destaque</label>
          <button class="btn btn-primary btn-small" data-acao="aprovar">Aprovar</button>
          <button class="btn btn-small btn-perigo" data-acao="rejeitar">Rejeitar</button>
        </div>
      </div>`).join("");
  } catch (e) {
    alvo.innerHTML = EventosUI.vazio(e.status === 403 ? "Esta conta não tem permissão de moderador." : escapeHtml(e.message));
  }
}

document.addEventListener("DOMContentLoaded", () => {
  carregarPendentes();
  document.getElementById("pendentes").addEventListener("click", async (ev) => {
    const botao = ev.target.closest("[data-acao]");
    if (!botao) return;
    const item = botao.closest(".mod-item");
    botao.disabled = true;
    try {
      await Api.moderarEvento(item.dataset.id, {
        novoStatus: STATUS[botao.dataset.acao],
        destaque: item.querySelector("[data-destaque]").checked,
      });
      mostrarToast(botao.dataset.acao === "aprovar" ? "Evento aprovado e publicado." : "Evento rejeitado.");
      carregarPendentes();
    } catch (e) {
      mostrarToast(e.message, "erro");
      botao.disabled = false;
    }
  });
});
