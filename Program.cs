using Microsoft.EntityFrameworkCore;
using API_PRODUCAO.Data;

var builder = WebApplication.CreateBuilder(args);

// Adiciona os serviços ao contêiner.
builder.Services.A­ddControllers();

// ** A CORREÇÃO ESTÁ AQUI **
// Registra os serviços do Swashbuckle, que são necessários para o app.UseSwagger().
builder.Services.A­ddSwaggerGen();

// Configura o DbContext com a string de conexão do appsettings.json
builder.Services.A­ddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configura o pipeline de requisições HTTP.
if (app.Environment.IsDevelopment())
{
    // Habilita o middleware que gera o arquivo swagger.json (do Swashbuckle).
    app.UseSwagger();

    // Middleware para redirecionar a raiz "/" para a documentação do Scalar
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.Value == "/")
        {
            context.Response.Redirect("/docs");
            return;
        }
        await next.Invoke();
    });

    // Endpoint que serve a UI do Scalar, lendo o JSON gerado pelo UseSwagger().
    app.MapGet("/docs", () =>
    {
        var html = @"
            <!doctype html>
            <html>
              <head>
                <title>API Reference | Valedourado</title>
                <meta charset=""utf-8"" />
                <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
              </head>
              <body>
                <script 
                  id=""api-reference"" 
                  data-url=""/swagger/v1/swagger.json"">
                </script>
                <script src=""https://cdn.jsdelivr.net/npm/@scalar/api-reference""></script>
              </body>
            </html>";

        return Results.Content(html, "text/html");
    }).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();