using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechFlow.Eventos.Core.Demonstracao;
using TechFlow.Eventos.Core.Imagens;
using TechFlow.Eventos.Core.Ingressos;
using TechFlow.Eventos.Core.Loja;
using TechFlow.Eventos.Core.Models;
using TechFlow.Eventos.Core.Pagamentos;
using TechFlow.Eventos.Core.Planos;

namespace TechFlow.Eventos.Infra.Data;

/// <summary>
/// Modo demonstração (Seed:EventosDemo=true). Mantém eventos, produtos e compras de exemplo sempre atualizados:
/// as datas dos eventos são recalculadas a partir de hoje, então a agenda nunca fica vazia. Todos os dados de pagamento são fictícios.
/// </summary>
public static class DadosDemonstracao
{
    public static bool Ativa(IConfiguration config) => bool.TryParse(config["Seed:EventosDemo"], out var ligado) && ligado;

    /// <summary>Cria o que faltar (eventos com imagem, contas de demonstração, produtos e compras) e acerta as datas.</summary>
    /// <param name="senhaDemo">Seed:DemoSenha. Se informada, as contas de demonstração podem ser usadas na apresentação; senão têm senha aleatória (ninguém entra).</param>
    public static async Task AplicarAsync(AppDbContext db, UserManager<ApplicationUser> users, ILogger log, string? senhaDemo = null)
    {
        await AtualizarEventosAsync(db);

        var vendedor = await GarantirContaAsync(users, log, CatalogoDemo.EmailVendedor, "Vendedores de demonstração", senhaDemo);
        var comprador = await GarantirContaAsync(users, log, CatalogoDemo.EmailComprador, "Cliente de demonstração", senhaDemo);
        if (vendedor is null || comprador is null) return;

        await GarantirLojaAsync(db, vendedor);
        await GarantirComprasAsync(db, comprador, vendedor);
    }

    /// <summary>
    /// Cria os eventos de demonstração que não existem e move os existentes para "hoje + N dias". Eventos sem imagem ganham a ilustração do tema.
    /// Não mexe na situação (aprovado, pendente...) de eventos já gravados, para não desfazer a moderação.
    /// </summary>
    public static async Task<int> AtualizarEventosAsync(AppDbContext db, CancellationToken ct = default)
    {
        var hoje = Evento.HojeBrasil();
        var titulos = CatalogoDemo.Eventos.Select(e => e.Titulo).ToList();
        var existentes = await db.Eventos
            .Where(e => (e.ContatoEnvio == CatalogoDemo.ContatoDemo || e.ContatoEnvio == CatalogoDemo.ContatoAntigo) && titulos.Contains(e.Titulo))
            .ToListAsync(ct);

        foreach (var demo in CatalogoDemo.Eventos)
        {
            var evento = existentes.FirstOrDefault(e => e.Titulo == demo.Titulo);
            if (evento is null)
            {
                evento = CatalogoDemo.CriarEvento(demo, hoje);
                db.Eventos.Add(evento);
            }
            else
            {
                evento.Data = CatalogoDemo.DataDoEvento(hoje, demo);
                evento.ContatoEnvio = CatalogoDemo.ContatoDemo;
            }

            if (!evento.TemFoto)
            {
                evento.Foto = new EventoFoto { Dados = ImagemDemo.GerarEvento(demo.Tema, demo.CorTopo, demo.CorBase), ContentType = "image/png" };
                evento.TemFoto = true;
            }
        }

        return await db.SaveChangesAsync(ct);
    }

    /// <summary>Boleto e Pix pendentes de demonstração vencem; renova o vencimento para a apresentação não ficar com cobrança expirada.</summary>
    public static async Task<int> AtualizarPagamentosPendentesAsync(AppDbContext db, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;
        var pendentes = await db.Pagamentos
            .Where(p => p.Status == StatusPagamento.Pendente && p.Gateway == "simulado" && p.UsuarioNome == "Cliente de demonstração" && p.VenceEm < agora.AddDays(2))
            .ToListAsync(ct);

        foreach (var p in pendentes.Where(p => p.Metodo == MetodoPagamento.Boleto))
        {
            p.VenceEm = agora.AddDays(3);
            p.CodigoBoleto = SimulacaoPagamento.LinhaDigitavel(p);
        }

        return await db.SaveChangesAsync(ct);
    }

    private static async Task<ApplicationUser?> GarantirContaAsync(UserManager<ApplicationUser> users, ILogger log, string email, string nome, string? senhaDemo)
    {
        var conta = await users.FindByEmailAsync(email);
        if (conta is not null) return conta;

        conta = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, NomeCompleto = nome, DataCadastro = DateTime.UtcNow };
        var senha = string.IsNullOrWhiteSpace(senhaDemo) ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)) + "aA1" : senhaDemo;
        var r = await users.CreateAsync(conta, senha);
        if (!r.Succeeded)
        {
            log.LogError("Não foi possível criar a conta de demonstração {Email}: {Erros}", email, string.Join(" ", r.Errors.Select(e => e.Description)));
            return null;
        }

        await users.AddToRoleAsync(conta, DbSeeder.RoleCliente);
        return conta;
    }

    private static async Task GarantirLojaAsync(AppDbContext db, ApplicationUser vendedor)
    {
        var existentes = await db.Produtos.Where(p => p.VendedorId == vendedor.Id).Select(p => p.Titulo).ToListAsync();
        var ordem = 0;
        foreach (var d in CatalogoDemo.Produtos)
        {
            ordem++;
            if (existentes.Contains(d.Titulo)) continue;

            db.Produtos.Add(new Produto
            {
                VendedorId = vendedor.Id, VendedorNome = d.Vendedor, Titulo = d.Titulo, Descricao = d.Descricao,
                Categoria = d.Categoria, Preco = d.Preco, Estoque = d.Estoque, Cidade = d.Cidade, Status = d.Status,
                CriadoEm = DateTime.UtcNow.AddMinutes(-ordem), // mantém a ordem da vitrine estável
                TemFoto = true,
                Foto = new ProdutoFoto { Dados = ImagemDemo.Gerar(d.CorTopo, d.CorBase), ContentType = "image/png" }
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Compras de exemplo da conta de demonstração, cobrindo produtos, ingressos e plano, com cartão de crédito, cartão de débito, Pix e boleto.
    /// Todos os dados de pagamento são fictícios. Só cria se a conta ainda não tem pedidos.
    /// </summary>
    private static async Task GarantirComprasAsync(AppDbContext db, ApplicationUser comprador, ApplicationUser vendedor)
    {
        if (await db.Pedidos.AnyAsync(p => p.CompradorId == comprador.Id)) return;

        var agora = DateTime.UtcNow;
        var produtos = await db.Produtos.Where(p => p.VendedorId == vendedor.Id).ToListAsync();
        var eventos = await db.Eventos.Where(e => e.ContatoEnvio == CatalogoDemo.ContatoDemo).ToListAsync();
        Produto? Prod(string titulo) => produtos.FirstOrDefault(p => p.Titulo == titulo);
        Evento? Ev(string titulo) => eventos.FirstOrDefault(e => e.Titulo == titulo);

        const string endereco = "Rua das Palmeiras, 120, Centro, Sacra Família do Tinguá, RJ (endereço fictício)";
        const string telefone = "(24) 90000-0000";

        // ---------- Produtos ----------
        var pedidos = new List<(Pedido Pedido, MetodoPagamento Metodo, StatusPagamento Status, int Parcelas, TimeSpan Atras)>();
        void Pedir(string titulo, int qtd, StatusPedido status, MetodoPagamento metodo, StatusPagamento pg, int parcelas, TimeSpan atras)
        {
            var p = Prod(titulo);
            if (p is null) return;
            var pedido = new Pedido
            {
                ProdutoId = p.Id, VendedorId = vendedor.Id, CompradorId = comprador.Id, CompradorNome = comprador.NomeCompleto,
                TituloProduto = p.Titulo, PrecoUnitario = p.Preco, Quantidade = qtd, Total = p.Preco * qtd,
                EnderecoEntrega = endereco, Telefone = telefone, Status = status,
                CriadoEm = agora - atras, AtualizadoEm = agora - atras + TimeSpan.FromHours(2)
            };
            db.Pedidos.Add(pedido);
            pedidos.Add((pedido, metodo, pg, parcelas, atras));
        }

        Pedir("Tapete de fibra de bananeira", 1, StatusPedido.Entregue, MetodoPagamento.CartaoCredito, StatusPagamento.Pago, 3, TimeSpan.FromDays(12));
        Pedir("Café especial torrado 250 g", 2, StatusPedido.Enviado, MetodoPagamento.Pix, StatusPagamento.Pago, 1, TimeSpan.FromDays(4));
        Pedir("Colar de sementes naturais", 1, StatusPedido.Pago, MetodoPagamento.CartaoDebito, StatusPagamento.Pago, 1, TimeSpan.FromDays(1));
        Pedir("Geleia de goiaba artesanal 300 g", 3, StatusPedido.AguardandoPagamento, MetodoPagamento.Boleto, StatusPagamento.Pendente, 1, TimeSpan.FromMinutes(20));

        // ---------- Ingressos ----------
        var ingressos = new List<(CompraIngresso Compra, MetodoPagamento? Metodo, StatusPagamento Status, TimeSpan Atras)>();
        void Ingresso(string titulo, int qtd, MetodoPagamento? metodo, StatusPagamento pg, TimeSpan atras)
        {
            var e = Ev(titulo);
            if (e is null) return;
            var confirmado = pg == StatusPagamento.Pago || e.Preco <= 0;
            ingressos.Add((new CompraIngresso
            {
                EventoId = e.Id, CompradorId = comprador.Id, CompradorNome = comprador.NomeCompleto, Quantidade = qtd,
                PrecoUnitario = e.Preco, Total = e.Preco * qtd, Codigo = Convert.ToHexString(RandomNumberGenerator.GetBytes(5)),
                Status = confirmado ? StatusIngresso.Confirmado : StatusIngresso.AguardandoPagamento,
                CriadoEm = agora - atras, ConfirmadoEm = confirmado ? agora - atras + TimeSpan.FromMinutes(2) : null
            }, metodo, pg, atras));
            db.ComprasIngresso.Add(ingressos[^1].Compra);
        }

        Ingresso("Roda de jongo na praça", 2, null, StatusPagamento.Pago, TimeSpan.FromHours(30)); // evento gratuito: sem cobrança
        Ingresso("Festival do Café e da Memória", 2, MetodoPagamento.Pix, StatusPagamento.Pago, TimeSpan.FromDays(2));
        Ingresso("Noite de choro e samba", 1, MetodoPagamento.CartaoDebito, StatusPagamento.Pago, TimeSpan.FromDays(1));
        Ingresso("Encontro de violeiros", 2, MetodoPagamento.Boleto, StatusPagamento.Pendente, TimeSpan.FromMinutes(45));

        // ---------- Plano ----------
        var assinatura = new AssinaturaPlano
        {
            UsuarioId = comprador.Id, Plano = PlanoId.Intermediario, Status = StatusAssinatura.Ativa,
            CriadaEm = agora.AddDays(-5), InicioEm = agora.AddDays(-5), ValidaAte = agora.AddDays(25)
        };
        db.AssinaturasPlano.Add(assinatura);

        await db.SaveChangesAsync(); // gera os ids usados nas cobranças

        Pagamento Cobranca(TipoPagamento tipo, int alvoId, decimal valor, string descricao, MetodoPagamento metodo, StatusPagamento status, int parcelas, TimeSpan atras)
        {
            var criado = agora - atras;
            var p = new Pagamento
            {
                Referencia = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant(), Tipo = tipo, AlvoId = alvoId,
                UsuarioId = comprador.Id, UsuarioNome = comprador.NomeCompleto, Valor = valor, Descricao = descricao, Status = status,
                Gateway = "simulado", CriadoEm = criado, PagoEm = status == StatusPagamento.Pago ? criado.AddMinutes(2) : null,
                Metodo = metodo, Parcelas = metodo == MetodoPagamento.CartaoCredito ? parcelas : 1
            };

            switch (metodo)
            {
                case MetodoPagamento.CartaoCredito: p.FinalCartao = "1111"; break;
                case MetodoPagamento.CartaoDebito: p.FinalCartao = "4242"; break;
                case MetodoPagamento.Pix:
                    p.VenceEm = status == StatusPagamento.Pendente ? agora.AddMinutes(30) : criado.AddMinutes(30);
                    p.CodigoPix = SimulacaoPagamento.CodigoPix(p);
                    break;
                case MetodoPagamento.Boleto:
                    p.VenceEm = status == StatusPagamento.Pendente ? agora.AddDays(3) : criado.AddDays(3);
                    p.CodigoBoleto = SimulacaoPagamento.LinhaDigitavel(p);
                    break;
            }

            return p;
        }

        foreach (var (pedido, metodo, status, parcelas, atras) in pedidos)
        {
            db.Pagamentos.Add(Cobranca(TipoPagamento.Pedido, pedido.Id, pedido.Total, $"Pedido #{pedido.Id}: {pedido.TituloProduto} (x{pedido.Quantidade})", metodo, status, parcelas, atras));
        }

        foreach (var (compra, metodo, status, atras) in ingressos)
        {
            if (metodo is null) continue;
            var titulo = eventos.First(e => e.Id == compra.EventoId).Titulo;
            db.Pagamentos.Add(Cobranca(TipoPagamento.Ingresso, compra.Id, compra.Total, $"Ingresso: {titulo} (x{compra.Quantidade})", metodo.Value, status, 1, atras));
        }

        db.Pagamentos.Add(Cobranca(TipoPagamento.Assinatura, assinatura.Id, 40m, "Plano Intermediário: 30 dias", MetodoPagamento.CartaoCredito, StatusPagamento.Pago, 1, TimeSpan.FromDays(5)));
        await db.SaveChangesAsync();
    }

    /// <summary>Liga a atualização periódica da demonstração, só quando o modo demonstração está ativo.</summary>
    public static IServiceCollection AddDemonstracao(this IServiceCollection services, IConfiguration config)
    {
        if (Ativa(config)) services.AddHostedService<DemonstracaoWorker>();
        return services;
    }
}

/// <summary>A cada 30 minutos recalcula as datas dos eventos e renova boletos pendentes de demonstração (a virada do dia é percebida em até 30 minutos).</summary>
public class DemonstracaoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<DemonstracaoWorker> _log;

    public DemonstracaoWorker(IServiceScopeFactory scopes, ILogger<DemonstracaoWorker> log)
    {
        _scopes = scopes;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var relogio = new PeriodicTimer(TimeSpan.FromMinutes(30));
        try
        {
            while (await relogio.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var alterados = await DadosDemonstracao.AtualizarEventosAsync(db, stoppingToken)
                                    + await DadosDemonstracao.AtualizarPagamentosPendentesAsync(db, stoppingToken);
                    if (alterados > 0) _log.LogInformation("Demonstração: {Qtd} registro(s) atualizado(s).", alterados);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogWarning(ex, "Falha ao atualizar a demonstração.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // desligamento da aplicação
        }
    }
}
