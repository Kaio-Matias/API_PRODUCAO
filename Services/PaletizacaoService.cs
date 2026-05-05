using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Valedourado.Shared.Dtos;

namespace API_PRODUCAO.Services
{
    public class PaletizacaoService : IPaletizacaoService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public PaletizacaoService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Paletizacao> CreatePaleteAsync(CreatePaleteDto paleteDto)
        {
            var palete = _mapper.Map<Paletizacao>(paleteDto);

            var ultimoPalete = await _context.Paletizacoes
                .Where(p => p.OrdemProducao == palete.OrdemProducao)
                .OrderByDescending(p => p.N_Palete)
                .FirstOrDefaultAsync();

            palete.N_Palete = (ultimoPalete?.N_Palete ?? 0) + 1;
            palete.DataHoraPaletizacao = System.DateTime.Now;

            _context.Paletizacoes.Add(palete);
            await _context.SaveChangesAsync();
            return palete;
        }

        public async Task<IEnumerable<Paletizacao>> GetPaletesByOpAsync(int ordemProducao)
        {
            return await _context.Paletizacoes
                .Where(p => p.OrdemProducao == ordemProducao)
                .OrderBy(p => p.N_Palete)
                .ToListAsync();
        }

        public async Task<PaleteDto?> UpdateQtdePaleteAsync(int paleteId, UpdateQtdePaleteDto updateDto)
        {
            var palete = await _context.Paletizacoes
                .Include(p => p.Producao)
                .FirstOrDefaultAsync(p => p.Id == paleteId);

            if (palete == null) return null;
            if (palete.Producao?.Status != "Aberto") return null;

            palete.QtdePorPalete = updateDto.NovaQtdePorPalete;
            palete.QtdeProduzida = updateDto.NovaQtdeProduzida;
            palete.Bloqueio = updateDto.NovoBloqueio;

            await _context.SaveChangesAsync();
            return _mapper.Map<PaleteDto>(palete);
        }

        public async Task<bool> DeletePaleteAsync(int id)
        {
            var palete = await _context.Paletizacoes
                .Include(p => p.Producao)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (palete == null) return false;
            if (palete.Producao?.Status != "Aberto") return false;

            _context.Paletizacoes.Remove(palete);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}