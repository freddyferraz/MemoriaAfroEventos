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
    /// sem senha fixa no código) e, se Seed:EventosDemo=true, os dados de demonstração (ver DadosDemonstracao).
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

        // Modo demonstração: eventos com datas relativas a hoje e produtos de exemplo na loja.
        if (DadosDemonstracao.Ativa(config))
        {
            await DadosDemonstracao.AplicarAsync(db, users, log, config["Seed:DemoSenha"]);
        }
    }
}
