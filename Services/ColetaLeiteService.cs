using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_PRODUCAO.Services
{
    public class ColetaLeiteService : IColetaLeiteService
    {
        private readonly AppDbContext _context;

        public ColetaLeiteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ColetaLeite>> GetByMesAnoAsync(int mes, int ano)
        {
            return await _context.ColetasLeite
                .Where(c => c.DataHoraColeta.Month == mes && c.DataHoraColeta.Year == ano)
                .OrderByDescending(c => c.DataHoraColeta)
                .ToListAsync();
        }

        public Task<bool> NumeroExistsAsync(string numeroColeta, int? exceptId = null)
        {
            var query = _context.ColetasLeite
                .Where(c => c.NumeroColeta == numeroColeta);

            if (exceptId.HasValue)
                query = query.Where(c => c.Id != exceptId.Value);

            return query.AnyAsync();
        }

        public async Task<ColetaLeite> CreateAsync(ColetaLeite coleta)
        {
            _context.ColetasLeite.Add(coleta);
            await _context.SaveChangesAsync();
            return coleta;
        }

        public async Task<ColetaLeite?> UpdateAsync(int id, ColetaLeite coleta)
        {
            var existing = await _context.ColetasLeite.FindAsync(id);
            if (existing == null) return null;

            existing.NumeroColeta = coleta.NumeroColeta;
            existing.DataHoraColeta = coleta.DataHoraColeta;
            existing.PlacaCaminhao = coleta.PlacaCaminhao;
            existing.Transportador = coleta.Transportador;
            existing.Linha = coleta.Linha;
            existing.Produtor = coleta.Produtor;
            existing.VolumeDescarregado = coleta.VolumeDescarregado;
            existing.VolumeDetalhado = coleta.VolumeDetalhado;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var coleta = await _context.ColetasLeite.FindAsync(id);
            if (coleta == null) return false;

            _context.ColetasLeite.Remove(coleta);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
