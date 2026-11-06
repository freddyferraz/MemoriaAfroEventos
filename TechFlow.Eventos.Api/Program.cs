using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TechFlow.Eventos.Infra;
using TechFlow.Eventos.Infra.Data;
using TechFlow.Eventos.Infra.Newsletter;

var builder = WebApplication.CreateBuilder(args);

// Render/Heroku entregam postgres://usuario:senha@host/db. Converte para o formato do Npgsql.
// Precisa vir ANTES do AddInfrastructure, que lê "DefaultConnection".
var rawConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(rawConn) &&
    (rawConn.StartsWith("postgres://") || rawConn.StartsWith("postgresql://")))
{
    var uri = new Uri(rawConn);
    var userInfo = uri.UserInfo.Split(':', 2);
    builder.Configuration["ConnectionStrings:DefaultConnection"] = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
        SslMode = Npgsql.SslMode.Prefer
    }.ToString();
}

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection (variável ConnectionStrings__DefaultConnection).");
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddNewsletterModule(builder.Configuration);

// Atrás do proxy do Render, o IP real do visitante vem em X-Forwarded-For (necessário para o limite por IP).
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// Limite de pedidos de inscrição na newsletter: 5 por hora por IP.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("newsletter", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1), QueueLimit = 0 }));
});

// ---------- JWT ----------
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Configure Jwt:Key com pelo menos 32 caracteres (variável Jwt__Key).");
}

builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    });
builder.Services.AddAuthorization();

// ---------- CORS (só necessário se o front ficar em outro domínio) ----------
const string corsPolicy = "Frontend";
builder.Services.AddCors(o => o.AddPolicy(corsPolicy, p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
     .AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "TechFlow Eventos API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Informe: Bearer {seu token JWT}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();
app.UseForwardedHeaders();

// Porta definida pela plataforma (Render). Localmente vale o launchSettings/ASPNETCORE_URLS.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port)) app.Urls.Add($"http://0.0.0.0:{port}");

// ---------- Banco: migrations (se existirem) ou criação direta; depois seed ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.GetMigrations().Any()) await db.Database.MigrateAsync();
    else await db.Database.EnsureCreatedAsync();
}
await DbSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment() || bool.TryParse(app.Configuration["Swagger:Enabled"], out var swagger) && swagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// No Render o HTTPS é terminado pelo proxy; o redirect só faz sentido localmente.
if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(corsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
