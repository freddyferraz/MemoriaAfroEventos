using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Infra.Data;

public static class DbSeeder
{
    public const string RoleAdmin = "Admin";
    public const string RoleModerador = "Moderador";
    public const string RoleCliente = "Cliente";

    /// <summary>
    /// Cria as roles, o primeiro administrador (a partir de Seed:AdminEmail / Seed:AdminSenha,
    /// sem senha fixa no código) e, se Seed:EventosDemo=true, eventos fictícios para demonstração.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var config = sp.GetRequiredService<IConfiguration>();
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var db = sp.GetRequiredService<AppDbContext>();

        foreach (var role in new[] { RoleAdmin, RoleModerador, RoleCliente })
        {
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));
        }

        var email = config["Seed:AdminEmail"];
        var senha = config["Seed:AdminSenha"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            log.LogWarning("Seed:AdminEmail/Seed:AdminSenha não configurados: nenhum administrador foi criado.");
        }
        else if (await users.FindByEmailAsync(email) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = email, Email = email, EmailConfirmed = true,
                NomeCompleto = config["Seed:AdminNome"] ?? "Administrador"
            };
            var r = await users.CreateAsync(admin, senha);
            if (r.Succeeded) await users.AddToRoleAsync(admin, RoleAdmin);
            else log.LogError("Falha ao criar o administrador: {Erros}", string.Join(" ", r.Errors.Select(e => e.Description)));
        }

        if (bool.TryParse(config["Seed:EventosDemo"], out var demo) && demo && !await db.Eventos.AnyAsync())
        {
            await db.Eventos.AddRangeAsync(EventosDemo());
            await db.SaveChangesAsync();
        }
    }

    // Dados fictícios: servem apenas para demonstrar a plataforma.
    private static IEnumerable<Evento> EventosDemo()
    {
        var hoje = Evento.HojeBrasil();
        Evento E(string t, CategoriaEvento c, string cidade, string end, int dias, string h, decimal preco,
                 string atr, string acc, string ing, string org, string fonte, bool dest, StatusEvento st = StatusEvento.Aprovado) => new()
        {
            Titulo = t, Categoria = c, Cidade = cidade, Endereco = end,
            Data = hoje.AddDays(dias), Horario = TimeOnly.Parse(h), Preco = preco, Atracoes = atr, Acessibilidade = acc,
            Ingressos = ing, Organizador = org, FonteOficial = fonte, Destaque = dest, Status = st,
            ContatoEnvio = "contato@exemplo.com.br", TipoAutor = TipoAutorEvento.Organizador
        };

        return new[]
        {
            E("Roda de jongo na praça", CategoriaEvento.CulturaAfro, "Sacra Família do Tinguá", "Praça central", 2, "19:00", 0, "Grupo de jongo da comunidade e tambores", "Local plano; banheiro adaptado", "Entrada gratuita", "Associação Cultural do Tinguá", "Perfil oficial da associação", true),
            E("Festival do Café e da Memória", CategoriaEvento.Gastronomia, "Sacra Família do Tinguá", "Largo da Igreja Matriz", 5, "10:00", 15, "Degustação de cafés, contação de histórias e feira de artesanato", "Rampa de acesso", "Ingresso na porta", "Produtores locais", "Site da prefeitura", true),
            E("Caminhada pelo circuito histórico", CategoriaEvento.Patrimonio, "Sacra Família do Tinguá", "Saída da estação", 1, "08:30", 0, "Guia local e paradas em casarões do ciclo do café", "Trajeto com ladeiras", "Inscrição gratuita por mensagem", "Grupo de Memória Afro", "Projeto Memória Afro", true),
            E("Noite de choro e samba", CategoriaEvento.Musica, "Engenheiro Paulo de Frontin", "Rua principal, 120", 3, "20:00", 25, "Quarteto de choro e roda de samba aberta", "Térreo, sem degraus", "Bilheteria do local", "Casa de Cultura", "Instagram da Casa de Cultura", false),
            E("Feira de produtores rurais", CategoriaEvento.Feira, "Miguel Pereira", "Praça da Feira", 6, "07:00", 0, "Hortifrúti, queijos, doces e artesanato", "Terreno plano", "Entrada gratuita", "Cooperativa de produtores", "Página da cooperativa", false),
            E("Exposição fotográfica Raízes", CategoriaEvento.CulturaAfro, "Vassouras", "Museu Municipal", 9, "14:00", 10, "Fotografias e relatos de moradores", "Elevador e banheiro adaptado", "Bilheteria do museu", "Museu Municipal", "Site do museu", false),
            E("Feira de artesanato do Tinguá", CategoriaEvento.Feira, "Sacra Família do Tinguá", "Quadra da escola", 11, "09:00", 0, "Artesãos da região", "Não informado", "Entrada gratuita", "Morador (envio comunitário)", "", false, StatusEvento.Pendente)
        };
    }
}
