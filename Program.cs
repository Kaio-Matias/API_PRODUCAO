using API_PRODUCAO.Data;
using API_PRODUCAO.Mappings;
using API_PRODUCAO.Middleware;
using API_PRODUCAO.Services;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
// O 'using AutoMapper;' pode não ser necessário aqui, mas não faz mal tê-lo.
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ----- INÍCIO DA CORREÇÃO FINAL PARA O AUTOMAPPER -----

// Em vez de passar o tipo, passamos uma "ação" de configuração.
// Aqui dentro, nós explicitamente adicionamos nosso perfil de mapeamento.
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
    // Se você tivesse outros perfis, adicionaria aqui também:
    // cfg.AddProfile<OutroProfile>();
});

// ----- FIM DA CORREÇÃO FINAL -----


// Registra TODOS os serviços para injeção de dependência
builder.Services.AddScoped<IPaletizacaoService, PaletizacaoService>();
builder.Services.AddScoped<IProducaoService, ProducaoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ICadastroService, CadastroService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();