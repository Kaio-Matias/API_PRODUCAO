using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_PRODUCAO.Services
{
    public class CadastroService : ICadastroService
    {
        private readonly AppDbContext _context;

        public CadastroService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cadastro>> GetAllAsync()
        {
            return await _context.Cadastros.ToListAsync();
        }

        public async Task<Cadastro?> GetByCodBarraAsync(string codBarra)
        {
            return await _context.Cadastros
                .FirstOrDefaultAsync(c => c.CodBarra == codBarra);
        }

        public async Task<Cadastro?> GetByCodProdutoAsync(string codProduto)
        {
            return await _context.Cadastros
                .FirstOrDefaultAsync(c => c.CodProduto == codProduto);
        }
    }
}