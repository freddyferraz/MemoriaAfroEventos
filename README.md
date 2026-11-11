# TechFlow Eventos

Agenda cultural colaborativa. Organizadores e moradores divulgam eventos; um moderador aprova antes de publicar.
Piloto: Sacra Família do Tinguá e região (RJ).

## Funcionalidades do MVP

1. **Página inicial** (`/index.html`): apresentação, eventos em destaque, próximos eventos e programação semanal e mensal.
2. **Explorar eventos** (`/explorar.html`): busca com filtros de cidade, distância, data, categoria, preço e gratuidade.
3. **Detalhes do evento** (`/evento.html?id=`): data, horário, endereço, atrações, preço, acessibilidade, ingressos, organizador e fonte oficial.
4. **Divulgar evento** (`/divulgar.html`): formulário público; o evento nasce *Pendente* e só aparece após aprovação em `/moderar.html`.

**QR codes** (`/qr.html`, para moderadores): QR 1 aponta para a página principal e QR 2 para o cadastro de evento.

### Modo demonstração

Com `Seed:EventosDemo=true` (variável `Seed__EventosDemo`), a plataforma se mantém populada para apresentações:

- **Eventos com data relativa e imagem:** cada evento guarda "quantos dias à frente de hoje" (hoje, amanhã, em 5 dias... até 20 dias) e a data é recalculada a partir do dia atual (Brasília) ao iniciar e a cada 30 minutos. A agenda nunca esvazia, e há eventos de hoje (permitem testar avaliações). Cada evento ganha uma ilustração temática (tambor, café, casarão, feira...) gerada pelo próprio sistema. A situação (aprovado, pendente) de eventos já gravados não é alterada.
- **Loja de exemplo:** 10 produtos de 4 vendedores fictícios, em todas as categorias, com fotos geradas pelo sistema, incluindo um produto esgotado e um aguardando aprovação.
- **Compras de exemplo:** a conta de demonstração do comprador tem 4 pedidos (cartão de crédito parcelado, Pix, cartão de débito e boleto pendente), 4 compras de ingressos (grátis, Pix, débito e boleto pendente) e o plano Intermediário ativo. O vendedor de demonstração vê essas vendas. **Todos os dados de pagamento são fictícios.**
- **Contas de demonstração:** `comprador.demo@conectacultura.exemplo` e `vendedor.demo@conectacultura.exemplo`. Defina `Seed__DemoSenha` (8+ caracteres, maiúscula, minúscula e número) para poder entrar nelas na apresentação; sem isso, a senha é aleatória e ninguém entra.
- Os dados de demonstração só são criados quando faltam; não duplicam. Boletos pendentes de demonstração têm o vencimento renovado automaticamente. Em produção real, mantenha `Seed__EventosDemo=false`.

### Fase 6: ingressos e formas de pagamento

- **Ingressos** (página do evento): quem está logado compra de 1 a 10 ingressos do evento; evento gratuito gera o ingresso na hora. Os ingressos aparecem em `meus-ingressos.html` com um QR code por ingresso. Sem login, a página mostra um convite para entrar ou criar conta.
- **Formas de pagamento** (`pagamento.html`, modo `Pagamentos__Modo=Simulado`): cartão de crédito (com parcelamento), cartão de débito, Pix (QR code e "copia e cola" fictícios) e boleto (linha digitável e código de barras fictícios). Valem para pedidos da loja, ingressos e planos. O número do cartão fica só no navegador: o servidor recebe apenas a forma de pagamento, as parcelas e os 4 últimos dígitos. Há botões com cartões de teste, inclusive um que simula recusa.
- **Migration:** `dotnet ef migrations add AdicionaIngressosEFormasDePagamento --project TechFlow.Eventos.Infra --startup-project TechFlow.Eventos.Api`

### Fase 5: loja local, planos para organizadores e pagamentos

Três módulos novos (`AddLojaModule`, `AddPlanosModule`, `AddPagamentosModule`):

- **Loja** (`loja.html`): qualquer pessoa vê os produtos; **anunciar e comprar exigem conta**. Anúncio novo passa por moderação (seção "Produtos pendentes" em `moderar.html`). Cada pedido é de um produto; o estoque é reservado na compra e volta se o pedido for cancelado. O vendedor vê endereço e telefone do comprador só depois do pagamento. Páginas: `loja`, `produto`, `anunciar-produto`, `meus-produtos`, `meus-pedidos`.
- **Planos** (`planos.html`): ver os planos é público; contratar exige conta. Free (1 destaque/mês, R$ 0,00), Básico (2, R$ 20,00), Intermediário (5, R$ 40,00) e Avançado (10, R$ 80,00). Os planos pagos valem 30 dias após a confirmação do pagamento; todas as contas começam no Free. Para mudar preços ou cotas, edite `Core/Planos/Planos.cs` (`CatalogoPlanos`). Ao divulgar um evento logado, o organizador marca "Usar um destaque do meu plano": a cota é do mês civil (renova no dia 1º), o evento rejeitado devolve o destaque, e o evento aprovado sai em destaque no carrossel.
- **Pagamentos**: `Pagamentos:Modo` define como se paga. **Desativado** (padrão): ninguém compra nem assina. **Simulado**: demonstração, a própria pessoa "aprova" na tela e nada é cobrado de verdade. **Manual**: a pessoa paga por fora (ex.: Pix) seguindo `Pagamentos:Instrucoes`, e um administrador confirma em `moderar.html`. Para usar um gateway real (Mercado Pago, Stripe, Asaas), implemente `IPagamentoGateway` e chame `PagamentoService.ConfirmarAsync` a partir do webhook do provedor.
- **Variáveis:** `Pagamentos__Modo` (Desativado, Simulado ou Manual) e `Pagamentos__Instrucoes` (texto do modo Manual).
- **Migration:** `dotnet ef migrations add AdicionaLojaPlanosPagamentos --project TechFlow.Eventos.Infra --startup-project TechFlow.Eventos.Api`

### Fase 4b: contas, carrossel e aprovação de cadastros

- **Criar conta** (`/cadastro.html`): a conta nasce *Pendente* (role Cliente) e só consegue entrar depois que um moderador aprova em `moderar.html` (seção "Cadastros pendentes"). O login de conta pendente ou rejeitada responde 403 com a situação. Limite de 5 cadastros por hora por IP. Se o módulo Newsletter estiver com e-mail configurado, a pessoa recebe um aviso na aprovação.
- **Meus interesses** só para quem está logado: as categorias e cidades escolhidas ficam na conta (tabela `PreferenciasUsuario`); os sinais de uso (abrir evento, agenda, boa avaliação) ficam no navegador, por conta, e só são registrados com login. `POST /api/recomendacoes` passou a exigir login. Quem não está logado vê um convite para entrar ou criar conta.
- **Carrossel:** "Em destaque" (deslizando devagar e sem parar a 30 px/s, ajustável pelo atributo `data-velocidade`; há botão Pausar, ele para ao passar o mouse, focar ou tocar, e fica desligado para quem prefere menos movimento) e "Recomendado para você". Rolagem por toque, setas e teclado (`js/carrossel.js`).
- **Divulgar evento:** logado, o campo de contato já vem com o nome da pessoa.
- **Migration:** `dotnet ef migrations add AdicionaContasEPreferencias --project TechFlow.Eventos.Infra --startup-project TechFlow.Eventos.Api`. A coluna `Status` de `AspNetUsers` usa `Aprovado = 0` como padrão, então administrador e moderadores existentes continuam podendo entrar.

### Fase 4: recomendações, calendário e avaliações

Três módulos independentes (cada um com `AddXModule` no `Program.cs`):

- **Recomendações** (`Core/Recomendacoes`): seção "Recomendado para você" no início e "Você também pode gostar" nos detalhes. O perfil de interesses fica **só no navegador** (localStorage): categorias e cidades escolhidas pela pessoa, mais sinais de uso (abrir um evento, adicionar à agenda, avaliar bem). O navegador envia o perfil em `POST /api/recomendacoes` e o servidor não o guarda. A pontuação é simples e explicável (categoria, cidade, destaque e proximidade da data), e cada sugestão mostra o motivo.
- **Calendário** (`Core/Calendario`): na página do evento, botões para Google Agenda, Outlook.com, Microsoft 365 e arquivo `.ics` (Apple e demais). Em Explorar, é possível assinar um feed `.ics` com a categoria e a cidade filtradas (`GET /api/calendario/feed.ics`). O cadastro não tem horário de término, então a agenda reserva 2 horas; o `.ics` inclui lembrete 1 hora antes.
- **Avaliações** (`Core/Avaliacoes`): nota de 1 a 5 e comentário, só para eventos que já aconteceram. A nota sem comentário é publicada na hora; o comentário passa pela moderação em `moderar.html`. Limite de 10 envios por hora por IP, campo isca anti-robô e bloqueio de links. A nota média aparece nos cards.
- **Migration:** `dotnet ef migrations add AdicionaAvaliacoes --project TechFlow.Eventos.Infra --startup-project TechFlow.Eventos.Api` (só o módulo de avaliações cria tabela).

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
