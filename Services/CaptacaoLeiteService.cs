using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_PRODUCAO.Services
{
    public class CaptacaoLeiteService : ICaptacaoLeiteService
    {
        private readonly AppDbContext _context;

        public CaptacaoLeiteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CaptacaoLeite>> GetByMesAnoAsync(int mes, int ano)
        {
            return await _context.CaptacoesLeite
                .Where(c => c.DataRecebimento.Month == mes && c.DataRecebimento.Year == ano)
                .OrderBy(c => c.DataRecebimento)
                .ToListAsync();
        }

        public async Task<CaptacaoLeite> CreateAsync(CaptacaoLeite captacao)
        {
            _context.CaptacoesLeite.Add(captacao);
            await _context.SaveChangesAsync();
            return captacao;
        }

        public async Task<CaptacaoLeite?> UpdateAsync(int id, CaptacaoLeite captacao)
        {
            var existing = await _context.CaptacoesLeite.FindAsync(id);
            if (existing == null) return null;

            existing.DataRecebimento = captacao.DataRecebimento;
            existing.VolumeRecebido = captacao.VolumeRecebido;
            existing.Observacao = captacao.Observacao;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var captacao = await _context.CaptacoesLeite.FindAsync(id);
            if (captacao == null) return false;
            _context.CaptacoesLeite.Remove(captacao);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
