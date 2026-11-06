# TechFlow Eventos

Agenda cultural colaborativa. Organizadores e moradores divulgam eventos; um moderador aprova antes de publicar.
Piloto: Sacra Família do Tinguá e região (RJ).

## Funcionalidades do MVP

1. **Página inicial** (`/index.html`): apresentação, eventos em destaque, próximos eventos e programação semanal e mensal.
2. **Explorar eventos** (`/explorar.html`): busca com filtros de cidade, distância, data, categoria, preço e gratuidade.
3. **Detalhes do evento** (`/evento.html?id=`): data, horário, endereço, atrações, preço, acessibilidade, ingressos, organizador e fonte oficial.
4. **Divulgar evento** (`/divulgar.html`): formulário público; o evento nasce *Pendente* e só aparece após aprovação em `/moderar.html`.

**QR codes** (`/qr.html`, para moderadores): QR 1 aponta para a página principal e QR 2 para o cadastro de evento.

### Fase 3: newsletter por preferência (módulo Newsletter)

- **Inscrição** (`/newsletter.html`): e-mail, categorias e cidades de interesse (vazio = todas) e aceite do consentimento (LGPD).
- **Confirmação (double opt-in):** a pessoa recebe um e-mail com link de confirmação. Só assinantes confirmados recebem eventos.
- **Envio:** quando um moderador **aprova** um evento, ele entra numa fila e o módulo envia um e-mail aos assinantes cujas preferências combinam. Cada assinante recebe o mesmo evento no máximo uma vez, e eventos já passados não são enviados.
- **Cancelamento:** link no rodapé de todo e-mail; remove o assinante e o histórico de envios.
- **Proteções:** limite de 5 pedidos de inscrição por hora por IP; a resposta de inscrição não revela se o e-mail já existe.
- **Modularidade:** o módulo vive em `Core/Newsletter` e `Infra/Newsletter`. O módulo de Eventos só conhece a interface `IEventoPublicadoHandler`; o módulo se liga em uma linha (`AddNewsletterModule`) e se remove apagando essa linha.
- **Migration:** `dotnet ef migrations add AdicionaNewsletter --project TechFlow.Eventos.Infra --startup-project TechFlow.Eventos.Api`

**Envio de e-mail (Brevo).** O Render bloqueia SMTP (portas 25, 465 e 587) em serviços gratuitos, então o envio usa a API HTTP do Brevo (plano gratuito: 300 e-mails/dia). Crie a conta, verifique um remetente em *Senders, Domains & Dedicated IPs* e gere uma chave de API. Variáveis:

| Variável | Uso |
|---|---|
| `App__BaseUrl` | endereço público do site (links dos e-mails), ex.: `https://seu-app.onrender.com` |
| `Email__Brevo__ApiKey` | chave de API do Brevo |
| `Email__RemetenteEmail` | e-mail remetente já verificado no Brevo |
| `Email__RemetenteNome` | nome exibido (padrão: TechFlow Eventos) |

Sem `ApiKey` e remetente, o sistema não envia nada: os e-mails só aparecem no log (útil em desenvolvimento, pois os links de confirmação ficam visíveis no console).

### Fase 2: foto do evento

- **Divulgar evento:** campo opcional de foto (JPEG, PNG ou WebP). O navegador reduz a imagem (lado maior de 1280 px, JPEG) antes de enviar; o servidor aceita até 2 MB e confere o formato pelo conteúdo do arquivo, não pelo nome.
- **Armazenamento:** a foto fica no banco, na tabela `Fotos` (bytea), separada de `Eventos` para que as listagens não carreguem imagens. `Eventos.TemFoto` indica se existe foto.
- **Exibição:** `GET /api/eventos/{id}/foto`. Pública para eventos aprovados (cache de 1 dia); para pendentes só moderadores, e a tela de moderação mostra a foto antes da aprovação. A foto aparece no card (Início e Explorar) e nos detalhes.
- **Migration:** `dotnet ef migrations add AdicionaFotoEvento --project TechFlow.Eventos.Infra --startup-project TechFlow.Eventos.Api`

## Arquitetura (Clean Architecture, .NET 8)

```
TechFlow.Eventos.sln
├── TechFlow.Eventos.Core/    Domínio: Evento, filtros, enums, geo (Haversine) e interfaces. Sem dependências.
├── TechFlow.Eventos.Infra/   EF Core + Postgres, Identity, repositório, JWT, leitura de links do Google Maps, seed.
└── TechFlow.Eventos.Api/     Controllers, DTOs, Program.cs e o front estático (wwwroot).
```

Principais endpoints: `GET /api/eventos` (filtros: `q, cidade, categoria, de, ate, precoMax, gratuito, destaque, lat, lng, distanciaKm`),
`GET /api/eventos/{id}`, `POST /api/eventos` (multipart/form-data), `GET /api/eventos/{id}/foto`, `GET /api/eventos/pendentes`, `PATCH /api/eventos/{id}/moderar`,
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
- Newsletter: a fila de envio é em memória (se a aplicação reiniciar com envios pendentes, eles se perdem); não há edição de preferências depois de confirmado (a pessoa cancela e se inscreve de novo); falta uma página de política de privacidade para linkar no consentimento.
- Testes automatizados.
