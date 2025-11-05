using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Services.Interfaces;
using Valedourado.Shared.Dtos;
using API_PRODUCAO.Models;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProducoesController : ControllerBase
    {
        private readonly IProducaoService _producaoService;
        private readonly IMapper _mapper;

        public ProducoesController(IProducaoService producaoService, IMapper mapper)
        {
            _producaoService = producaoService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProducaoDto>>> GetProducoes()
        {
            var producoes = await _producaoService.GetAllProducoesAsync();
            var producoesDto = _mapper.Map<IEnumerable<ProducaoDto>>(producoes);
            return Ok(producoesDto);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProducaoDto>> GetProducao(int id)
        {
            var producao = await _producaoService.GetProducaoByIdAsync(id);
            if (producao == null)
            {
                return NotFound();
            }
            var producaoDto = _mapper.Map<ProducaoDto>(producao);
            return Ok(producaoDto);
        }

        [HttpGet("abertas")]
        public async Task<ActionResult<IEnumerable<ProducaoDto>>> GetProducoesAbertas()
        {
            var producoesAbertas = await _producaoService.GetProducoesAbertasAsync();
            var producoesDto = _mapper.Map<IEnumerable<ProducaoDto>>(producoesAbertas);
            return Ok(producoesDto);
        }

        // ===== NOVO ENDPOINT PARA OPs FECHADAS =====
        [HttpGet("closed/bydate")]
        public async Task<ActionResult<IEnumerable<ProducaoDto>>> GetClosedProducoesPorData([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var producoes = await _producaoService.GetClosedProducoesByDateRangeAsync(startDate, endDate);
                var producoesDto = _mapper.Map<IEnumerable<ProducaoDto>>(producoes);
                return Ok(producoesDto);
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocorreu um erro interno ao buscar as produções fechadas por data.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ProducaoDto>> PostProducao([FromBody] CreateProducaoDto producaoDto)
        {
            var producao = _mapper.Map<Producoes>(producaoDto);
            var producaoCriada = await _producaoService.CreateProducaoAsync(producao);
            var resultadoDto = _mapper.Map<ProducaoDto>(producaoCriada);
            return CreatedAtAction(nameof(GetProducao), new { id = resultadoDto.OrdemProducao }, resultadoDto);
        }

        [HttpPut("{ordemProducao}/fechar")]
        public async Task<IActionResult> FecharProducao(int ordemProducao)
        {
            var sucesso = await _producaoService.FecharProducaoAsync(ordemProducao);
            if (!sucesso)
            {
                return NotFound($"OP {ordemProducao} não encontrada ou já está fechada.");
            }
            return Ok(new { Message = $"Ordem de Produção {ordemProducao} fechada com sucesso." });
        }

        // ===== NOVO ENDPOINT ADICIONADO =====
        [HttpPut("{ordemProducao}/cancelar")]
        public async Task<IActionResult> CancelarProducao(int ordemProducao)
        {
            var sucesso = await _producaoService.CancelarProducaoAsync(ordemProducao);
            if (!sucesso)
            {
                // Retorna 404 (Não Encontrado) se a OP não existir ou não puder ser cancelada
                return NotFound($"OP {ordemProducao} não encontrada ou não pode ser cancelada (pode já estar fechada/cancelada).");
            }
            return Ok(new { Message = $"Ordem de Produção {ordemProducao} cancelada com sucesso." });
        }
        // ===================================
    }
}