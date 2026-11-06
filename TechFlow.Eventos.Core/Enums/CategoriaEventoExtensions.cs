namespace TechFlow.Eventos.Core.Enums;

public static class CategoriaEventoExtensions
{
    public static string Nome(this CategoriaEvento c) => c switch
    {
        CategoriaEvento.CulturaAfro => "Cultura afro",
        CategoriaEvento.Musica => "Música",
        CategoriaEvento.Gastronomia => "Gastronomia",
        CategoriaEvento.Patrimonio => "Patrimônio",
        CategoriaEvento.Feira => "Feira",
        _ => "Outros"
    };
}
