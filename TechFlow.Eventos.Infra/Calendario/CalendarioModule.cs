using Microsoft.Extensions.DependencyInjection;

namespace TechFlow.Eventos.Infra.Calendario;

/// <summary>Módulo Calendário: não tem estado nem tabelas (a geração do .ics é lógica pura no Core).</summary>
public static class CalendarioModule
{
    public static IServiceCollection AddCalendarioModule(this IServiceCollection services) => services;
}
