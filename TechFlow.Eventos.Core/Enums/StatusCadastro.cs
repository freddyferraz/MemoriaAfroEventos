namespace TechFlow.Eventos.Core.Enums;

/// <summary>
/// Situação do cadastro de uma conta. Aprovado vale 0 de propósito: as contas que já existiam antes deste
/// recurso (administrador, moderadores) passam a valer Aprovado na migration, sem precisar de ajuste manual.
/// Contas criadas pelo formulário público nascem Pendente e só entram depois da aprovação de um moderador.
/// </summary>
public enum StatusCadastro
{
    Aprovado = 0,
    Pendente = 1,
    Rejeitado = 2
}
