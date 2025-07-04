using Microsoft.EntityFrameworkCore;
using API_PRODUCAO.Data;
using API_PRODUCAO.Services.Interfaces;
using API_PRODUCAO.Services;
using API_PRODUCAO.Middleware;
using API_PRODUCAO.Mappings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registra o AutoMapper, buscando os perfis de mapeamento no projeto
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Registra TODOS os serviços para injeção de dependência
builder.Services.AddScoped<IPaletizacaoService, PaletizacaoService>();
builder.Services.AddScoped<IProducaoService, ProducaoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ICadastroService, CadastroService>();
// Adicione aqui os outros serviços que criar (ICadastroService, etc.)



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