namespace TechFlow.Eventos.Core.Compartilhado;

public enum TipoErro
{
    Invalido,
    NaoEncontrado,
    Conflito,
    NaoPermitido,
    Indisponivel
}

/// <summary>Violação de uma regra de negócio. Os controllers a traduzem em 400, 404, 409, 403 ou 503.</summary>
public class RegraDeNegocioException : Exception
{
    public TipoErro Tipo { get; }

    public RegraDeNegocioException(TipoErro tipo, string mensagem) : base(mensagem) => Tipo = tipo;
}
