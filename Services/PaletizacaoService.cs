using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using API_PRODUCAO.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper; // CORREÇÃO 1: ADICIONE ESTE 'USING'

namespace API_PRODUCAO.Services
{
    public class PaletizacaoService : IPaletizacaoService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper; // CORREÇÃO 2: DECLARE O CAMPO PARA O MAPPER

        // CORREÇÃO 3: RECEBA O IMapper NO CONSTRUTOR
        public PaletizacaoService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper; // ATRIBUA A DEPENDÊNCIA AO CAMPO
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

            // Retorna a entidade completa que acabou de ser salva, com o ID e N_Palete gerados.
            return palete;
        }


        public async Task<Paletizacao> CreatePaleteAsync(Paletizacao palete)
        {
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

        public async Task<PaleteDto> UpdateQtdePaleteAsync(int paleteId, UpdateQtdePaleteDto updateDto)
        {
            var paleteParaAtualizar = await _context.Paletizacoes.FindAsync(paleteId);
            if (paleteParaAtualizar == null) return null;

            int qtdeCxOriginal = paleteParaAtualizar.QtdeCx;
            int producaoAntiga = qtdeCxOriginal * paleteParaAtualizar.QtdePorPalete;
            int producaoNova = qtdeCxOriginal * updateDto.NovaQtdePorPalete;
            int diferenca = producaoNova - producaoAntiga;

            paleteParaAtualizar.QtdePorPalete = updateDto.NovaQtdePorPalete;
            paleteParaAtualizar.QtdeProduzida += diferenca;

            var paletesSeguintes = await _context.Paletizacoes
                .Where(p => p.OrdemProducao == paleteParaAtualizar.OrdemProducao && p.N_Palete > paleteParaAtualizar.N_Palete)
                .ToListAsync();

            foreach (var p in paletesSeguintes)
            {
                p.QtdeProduzida += diferenca;
            }

            await _context.SaveChangesAsync();

            // Agora a variável _mapper existe e pode ser usada aqui.
            return _mapper.Map<PaleteDto>(paleteParaAtualizar);
        }
    }
}