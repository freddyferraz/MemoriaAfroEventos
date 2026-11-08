using Microsoft.EntityFrameworkCore;
using TechFlow.Eventos.Core.Pagamentos;
using TechFlow.Eventos.Infra.Data;

namespace TechFlow.Eventos.Infra.Pagamentos;

public class PagamentoRepository : IPagamentoRepository
{
    private readonly AppDbContext _db;

    public PagamentoRepository(AppDbContext db) => _db = db;

    public async Task AdicionarAsync(Pagamento pagamento) => await _db.Pagamentos.AddAsync(pagamento);

    public Task<Pagamento?> ObterPorReferenciaAsync(string referencia) =>
        _db.Pagamentos.FirstOrDefaultAsync(p => p.Referencia == referencia);

    public Task<Pagamento?> ObterPendentePorAlvoAsync(TipoPagamento tipo, int alvoId) =>
        _db.Pagamentos.FirstOrDefaultAsync(p => p.Tipo == tipo && p.AlvoId == alvoId && p.Status == StatusPagamento.Pendente);

    public async Task<IReadOnlyList<Pagamento>> ListarPendentesAsync() =>
        await _db.Pagamentos.AsNoTracking().Where(p => p.Status == StatusPagamento.Pendente).OrderBy(p => p.CriadoEm).ToListAsync();

    public Task SalvarAlteracoesAsync() => _db.SaveChangesAsync();
}
