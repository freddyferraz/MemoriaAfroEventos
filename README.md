# TechFlow Eventos

Agenda cultural colaborativa. Organizadores e moradores divulgam eventos; um moderador aprova antes de publicar.
Piloto: Sacra Família do Tinguá e região (RJ).

## Funcionalidades do MVP

1. **Página inicial** (`/index.html`): apresentação, eventos em destaque, próximos eventos e programação semanal e mensal.
2. **Explorar eventos** (`/explorar.html`): busca com filtros de cidade, distância, data, categoria, preço e gratuidade.
3. **Detalhes do evento** (`/evento.html?id=`): data, horário, endereço, atrações, preço, acessibilidade, ingressos, organizador e fonte oficial.
4. **Divulgar evento** (`/divulgar.html`): formulário público; o evento nasce *Pendente* e só aparece após aprovação em `/moderar.html`.

**QR codes** (`/qr.html`, para moderadores): QR 1 aponta para a página principal e QR 2 para o cadastro de evento.

## Arquitetura (Clean Architecture, .NET 8)

```
TechFlow.Eventos.sln
├── TechFlow.Eventos.Core/    Domínio: Evento, filtros, enums, geo (Haversine) e interfaces. Sem dependências.
├── TechFlow.Eventos.Infra/   EF Core + Postgres, Identity, repositório, JWT, leitura de links do Google Maps, seed.
└── TechFlow.Eventos.Api/     Controllers, DTOs, Program.cs e o front estático (wwwroot).
```

Principais endpoints: `GET /api/eventos` (filtros: `q, cidade, categoria, de, ate, precoMax, gratuito, destaque, lat, lng, distanciaKm`),
`GET /api/eventos/{id}`, `POST /api/eventos`, `GET /api/eventos/pendentes`, `PATCH /api/eventos/{id}/moderar`,
`POST /api/auth/login`, `POST /api/auth/moderadores` (Admin).

## Rodando localmente

Com Docker: `cp .env.example .env`, edite os valores e rode `docker compose up --build` (http://localhost:8080).

Sem Docker (precisa de um Postgres local):

```
dotnet run --project TechFlow.Eventos.Api
```

Em Development, `appsettings.Development.json` aponta para `localhost`, cria o admin `admin@local.test` (senha no arquivo, só para uso local) e carrega eventos fictícios.
Swagger em `/swagger`.

## Banco de dados e migrations

Sem migrations no projeto, a API cria as tabelas na primeira execução (`EnsureCreated`). Para versionar o esquema:

```
dotnet tool install --global dotnet-ef
dotnet ef migrations add Inicial --project TechFlow.Eventos.Infra --startup-project TechFlow.Eventos.Api
```

Depois disso a API passa a aplicar as migrations automaticamente ao iniciar.
Se você já subiu o banco com `EnsureCreated`, apague-o antes de adotar migrations.

## Produção (Render ou similar)

Nada de segredos no repositório. Configure por variáveis de ambiente:

| Variável | Uso |
|---|---|
| `ConnectionStrings__DefaultConnection` | string do Postgres (aceita `postgres://...` do Render) |
| `Jwt__Key` | chave aleatória com 32+ caracteres |
| `Seed__AdminEmail`, `Seed__AdminSenha` | primeiro administrador (criado só se informados) |
| `Seed__EventosDemo` | `true` carrega eventos fictícios (deixe `false` em produção) |
| `Swagger__Enabled` | `true` habilita o Swagger fora de Development |

O `Dockerfile` da raiz publica a API; a porta vem de `PORT`.

## Pendências conhecidas

- Paginação da lista de eventos e limite de envios por IP em `POST /api/eventos` (endpoint público).
- Eventos recorrentes (cada ocorrência é um cadastro).
- Testes automatizados.
