using TechFlow.Eventos.Core.Enums;
using TechFlow.Eventos.Core.Models;

namespace TechFlow.Eventos.Api.DTOs;

/// <summary>Conversão Evento para EventoDto usada pelos módulos novos (o EventosController mantém a sua própria cópia).</summary>
public static class EventoMapper
{
    public static EventoDto ParaDto(Evento e) => new()
    {
        Id = e.Id, Titulo = e.Titulo, Categoria = e.Categoria, CategoriaNome = e.Categoria.Nome(),
        Cidade = e.Cidade, Endereco = e.Endereco, Latitude = e.Latitude, Longitude = e.Longitude,
        Data = e.Data, Horario = e.Horario, Atracoes = e.Atracoes, Preco = e.Preco, Gratuito = e.Gratuito,
        Acessibilidade = e.Acessibilidade, Ingressos = e.Ingressos, Organizador = e.Organizador,
        FonteOficial = e.FonteOficial, Destaque = e.Destaque, TemFoto = e.TemFoto, Status = e.Status, TipoAutor = e.TipoAutor
    };
}
