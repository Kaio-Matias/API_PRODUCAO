using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_PRODUCAO.Services
{
    public class ProducaoService : IProducaoService
    {
        private readonly AppDbContext _context;

        public ProducaoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Producoes> CreateProducaoAsync(Producoes producao)
        {
            producao.Status = "Aberto";
            producao.DataHoraAbertura = DateTime.Now;
            producao.DataHoraFechamento = null;

            _context.Producoes.Add(producao);
            await _context.SaveChangesAsync();
            return producao;
        }

        public async Task<IEnumerable<Producoes>> GetAllProducoesAsync()
        {
            return await _context.Producoes.ToListAsync();
        }

        public async Task<Producoes?> GetProducaoByIdAsync(int id)
        {
            return await _context.Producoes.FindAsync(id);
        }

        public async Task<IEnumerable<Producoes>> GetProducoesAbertasAsync()
        {
            return await _context.Producoes
                .Where(p => p.Status == "Aberto")
                .ToListAsync();
        }
    }
}