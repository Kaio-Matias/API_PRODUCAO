using API_PRODUCAO.Data;
using API_PRODUCAO.Mappings;
using API_PRODUCAO.Services;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using API_PRODUCAO.Middleware;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using API_PRODUCAO.Security;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Datas recebidas em UTC ("...Z") são convertidas para hora local, padrão do banco.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new DataHoraLocalConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── CORS ─────────────────────────────────────────────────────────────────────
// React Native / mobile clients do not use CORS, but the web portal does.
// Origins are configured in appsettings.json → AllowedOrigins.
// In production, restrict this list to known portal hostnames.
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebPortal", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        else
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); // dev fallback
    });
});

// ── JWT Authentication ────────────────────────────────────────────────────────
// Pode vir do appsettings ou de variáveis de ambiente (Jwt__Key, Jwt__Issuer, Jwt__Audience).
var jwtKey    = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAud    = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32 ||
    string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAud))
{
    throw new InvalidOperationException(
        "Configuração JWT ausente ou fraca: defina Jwt:Key (mín. 32 caracteres), Jwt:Issuer e Jwt:Audience.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAud,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.FromMinutes(2),
        };
    });

var cargosGestao = new[] { Cargos.Supervisor, Cargos.Admin };

builder.Services.AddScoped<IAuthorizationHandler, CargoOuModuloHandler>();
builder.Services.AddAuthorization(options =>
{
    // Toda rota exige login, exceto as marcadas com [AllowAnonymous] (ex.: /api/Usuario/login)
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(Politicas.Administracao, p => p.RequireAuthenticatedUser()
        .AddRequirements(new CargoOuModuloRequirement(cargosGestao, Array.Empty<string>())));

    options.AddPolicy(Politicas.AberturaOp, p => p.RequireAuthenticatedUser()
        .AddRequirements(new CargoOuModuloRequirement(cargosGestao, new[] { "supervisor", "supervisor_abertura" })));

    options.AddPolicy(Politicas.GestaoOp, p => p.RequireAuthenticatedUser()
        .AddRequirements(new CargoOuModuloRequirement(cargosGestao, new[] { "supervisor", "supervisor_gestao", "supervisor_detalhes" })));
});

// Configure Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped<IProducaoService, ProducaoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ICadastroService, CadastroService>();
builder.Services.AddScoped<IPaletizacaoService, PaletizacaoService>();
builder.Services.AddScoped<IMetaProducaoService, MetaProducaoService>();
builder.Services.AddScoped<ICaptacaoLeiteService, CaptacaoLeiteService>();
builder.Services.AddScoped<IColetaLeiteService, ColetaLeiteService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();

// Coletas de leite lidas do banco do sistema do leite (appsettings → LeiteErp).
// Sem configuração o serviço fica inativo e o agendador não faz nada.
builder.Services.AddScoped<ColetaLeiteErpService>();
builder.Services.AddHostedService<ColetaLeiteSyncWorker>();

// AutoMapper
builder.Services.AddAutoMapper(cfg => {
    cfg.AddProfile<MappingProfile>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("WebPortal");

app.UseMiddleware<ExceptionHandlingMiddleware>();

// HTTPS redirect ativo em produção; desabilitado em dev para não quebrar o emulador Android.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
