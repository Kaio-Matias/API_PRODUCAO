// No arquivo /Services/PaletizacaoService.cs
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

        // Este é o método CORRETO que implementa a interface.
        public async Task<Paletizacao> CreatePaleteAsync(CreatePaleteDto paleteDto)
        {
            var palete = _mapper.Map<Paletizacao>(paleteDto);

            // ... sua lógica para calcular N_Palete e DataHora ...
            var ultimoPalete = await _context.Paletizacoes
                .Where(p => p.OrdemProducao == palete.OrdemProducao)
                .OrderByDescending(p => p.N_Palete)
                .FirstOrDefaultAsync();

            palete.N_Palete = (ultimoPalete?.N_Palete ?? 0) + 1;
            palete.DataHoraPaletizacao = System.DateTime.Now;

            _context.Paletizacoes.Add(palete);
            await _context.SaveChangesAsync();
            return palete; // Retorna a entidade completa salva
        }

        // ❌ REMOVA ESTE MÉTODO OBsoleto DA SUA CLASSE ❌
        /* public async Task<Paletizacao> CreatePaleteAsync(Paletizacao palete)
        {
             _context.Paletizacoes.Add(palete);
             await _context.SaveChangesAsync();
             return palete;
        }
        */

        // ... resto dos métodos (GetPaletesByOpAsync, UpdateQtdePaleteAsync) ...
        public async Task<IEnumerable<Paletizacao>> GetPaletesByOpAsync(int ordemProducao)
        {
            return await _context.Paletizacoes
                .Where(p => p.OrdemProducao == ordemProducao)
                .OrderBy(p => p.N_Palete)
                .ToListAsync();
        }

        public async Task<PaleteDto> UpdateQtdePaleteAsync(int paleteId, UpdateQtdePaleteDto updateDto)
        {
            // ... sua lógica de atualização ...
            var paleteParaAtualizar = await _context.Paletizacoes.FindAsync(paleteId);
            if (paleteParaAtualizar == null) return null;

            // ...

            await _context.SaveChangesAsync();
            return _mapper.Map<PaleteDto>(paleteParaAtualizar);
        }
    }
}