using API_PRODUCAO.Data;
using API_PRODUCAO.Mappings;
using API_PRODUCAO.Services;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using API_PRODUCAO.Middleware;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
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
var jwtKey    = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAud    = builder.Configuration["Jwt:Audience"]!;

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

builder.Services.AddAuthorization();

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
builder.Services.AddScoped<IRelatorioService, RelatorioService>();

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
