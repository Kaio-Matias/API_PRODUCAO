using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using API_PRODUCAO.Data;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer("Server=localhost;Database=DB_PRODUCAO;Trusted_Connection=True;TrustServerCertificate=True;");

using (var context = new AppDbContext(optionsBuilder.Options))
{
    var cadastros = context.Cadastro.ToList();
    foreach(var c in cadastros.Take(5)) {
        Console.WriteLine($"{c.CodProduto} - {c.Produto} - Cx: {c.QtdeCaixa}");
    }
}
