using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_PRODUCAO.Services
{
    public class MetaProducaoService : IMetaProducaoService
    {
        private readonly AppDbContext _context;

        public MetaProducaoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MetaProducao>> GetMetasByMesAnoAsync(int mes, int ano)
        {
            return await _context.MetasProducao
                .Where(m => m.Mes == mes && m.Ano == ano)
                .OrderBy(m => m.Classe)
                .ToListAsync();
        }

        public async Task<MetaProducao?> GetMetaByClasseAsync(int mes, int ano, string classe)
        {
            return await _context.MetasProducao
                .FirstOrDefaultAsync(m => m.Mes == mes && m.Ano == ano && m.Classe == classe);
        }

        public async Task<MetaProducao> UpsertMetaAsync(MetaProducao meta)
        {
            MetaProducao? existing;

            // Se tem CodProduto, upsert por produto; senão por classe
            if (!string.IsNullOrWhiteSpace(meta.CodProduto))
            {
                existing = await _context.MetasProducao
                    .FirstOrDefaultAsync(m => m.Mes == meta.Mes && m.Ano == meta.Ano && m.CodProduto == meta.CodProduto);
            }
            else
            {
                existing = await _context.MetasProducao
                    .FirstOrDefaultAsync(m => m.Mes == meta.Mes && m.Ano == meta.Ano
                                           && m.Classe == meta.Classe
                                           && m.CodProduto == null);
            }

            if (existing != null)
            {
                existing.MetaCaixas = meta.MetaCaixas;
                existing.MetaKg    = meta.MetaKg;
                existing.MetaRS    = meta.MetaRS;
                existing.Classe    = meta.Classe;
                await _context.SaveChangesAsync();
                return existing;
            }

            _context.MetasProducao.Add(meta);
            await _context.SaveChangesAsync();
            return meta;
        }

        public async Task<bool> DeleteMetaAsync(int id)
        {
            var meta = await _context.MetasProducao.FindAsync(id);
            if (meta == null) return false;
            _context.MetasProducao.Remove(meta);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<MetaCaptacaoLeite?> GetMetaCaptacaoAsync(int mes, int ano)
        {
            return await _context.MetasCaptacaoLeite
                .FirstOrDefaultAsync(m => m.Mes == mes && m.Ano == ano);
        }

        public async Task<MetaCaptacaoLeite> UpsertMetaCaptacaoAsync(MetaCaptacaoLeite meta)
        {
            var existing = await _context.MetasCaptacaoLeite
                .FirstOrDefaultAsync(m => m.Mes == meta.Mes && m.Ano == meta.Ano);

            if (existing != null)
            {
                existing.MetaLitros = meta.MetaLitros;
                await _context.SaveChangesAsync();
                return existing;
            }

            _context.MetasCaptacaoLeite.Add(meta);
            await _context.SaveChangesAsync();
            return meta;
        }
    }
}
