# Build com a raiz da solução como contexto:  docker build -t techflow-eventos .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore TechFlow.Eventos.Api/TechFlow.Eventos.Api.csproj
RUN dotnet publish TechFlow.Eventos.Api/TechFlow.Eventos.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
# A porta vem da variável PORT (Render) e é lida no Program.cs
ENTRYPOINT ["dotnet", "TechFlow.Eventos.Api.dll"]
