using Microsoft.AspNetCore.Identity;

namespace TechFlow.Eventos.Infra.Data;

/// <summary>Mensagens do ASP.NET Identity em português, exibidas no cadastro de contas.</summary>
public class IdentityErrorDescriberPt : IdentityErrorDescriber
{
    private static IdentityError E(string codigo, string descricao) => new() { Code = codigo, Description = descricao };

    public override IdentityError DefaultError() => E(nameof(DefaultError), "Não foi possível concluir a operação.");
    public override IdentityError DuplicateEmail(string email) => E(nameof(DuplicateEmail), "Já existe um cadastro com este e-mail.");
    public override IdentityError DuplicateUserName(string userName) => E(nameof(DuplicateUserName), "Já existe um cadastro com este e-mail.");
    public override IdentityError InvalidEmail(string? email) => E(nameof(InvalidEmail), "Informe um e-mail válido.");
    public override IdentityError InvalidUserName(string? userName) => E(nameof(InvalidUserName), "Informe um e-mail válido.");
    public override IdentityError PasswordTooShort(int length) => E(nameof(PasswordTooShort), $"A senha deve ter pelo menos {length} caracteres.");
    public override IdentityError PasswordRequiresDigit() => E(nameof(PasswordRequiresDigit), "A senha deve ter pelo menos um número.");
    public override IdentityError PasswordRequiresLower() => E(nameof(PasswordRequiresLower), "A senha deve ter pelo menos uma letra minúscula.");
    public override IdentityError PasswordRequiresUpper() => E(nameof(PasswordRequiresUpper), "A senha deve ter pelo menos uma letra maiúscula.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => E(nameof(PasswordRequiresNonAlphanumeric), "A senha deve ter pelo menos um caractere especial.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => E(nameof(PasswordRequiresUniqueChars), $"A senha deve ter pelo menos {uniqueChars} caracteres diferentes.");
}
