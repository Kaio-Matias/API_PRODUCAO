using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Services.Interfaces;
using Valedourado.Shared.Dtos;
using API_PRODUCAO.Models;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using API_PRODUCAO.Security;
using Microsoft.AspNetCore.Authorization;

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

        [Authorize(Policy = Politicas.AberturaOp)]
        [HttpPost]
        public async Task<ActionResult<ProducaoDto>> PostProducao([FromBody] CreateProducaoDto producaoDto)
        {
            var producao = _mapper.Map<Producoes>(producaoDto);
            var producaoCriada = await _producaoService.CreateProducaoAsync(producao);
            var resultadoDto = _mapper.Map<ProducaoDto>(producaoCriada);
            return CreatedAtAction(nameof(GetProducao), new { id = resultadoDto.OrdemProducao }, resultadoDto);
        }

        [Authorize(Policy = Politicas.GestaoOp)]
        [HttpPut("{id}")]
        public async Task<ActionResult<ProducaoDto>> PutProducao(int id, [FromBody] ProducaoDto producaoDto)
        {
            var producao = _mapper.Map<Producoes>(producaoDto);
            Producoes? updated;
            try
            {
                // Só o Portal envia este cabeçalho; demais apps não podem ajustar data/hora da OP
                var vemDoPortal = string.Equals(Request.Headers["X-Origem"].ToString(), "portal", StringComparison.OrdinalIgnoreCase);
                updated = await _producaoService.UpdateProducaoAsync(id, producao, vemDoPortal);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            if (updated == null) return NotFound();

            return Ok(_mapper.Map<ProducaoDto>(updated));
        }

        [Authorize(Policy = Politicas.GestaoOp)]
        [HttpPut("{ordemProducao}/fechar")]
        public async Task<IActionResult> FecharProducao(int ordemProducao, [FromQuery] string? supervisor = null)
        {
            // O nome registrado vem do login; o parâmetro só é usado se o token não tiver nome
            var sucesso = await _producaoService.FecharProducaoAsync(ordemProducao, NomeUsuarioLogado() ?? supervisor);
            if (!sucesso)
            {
                return NotFound($"OP {ordemProducao} não encontrada ou já está fechada.");
            }
            return Ok(new { Message = $"Ordem de Produção {ordemProducao} fechada com sucesso." });
        }

        /// <summary>Reabre OP Fechada/Cancelada, estornando o consumo de A Granel.</summary>
        [Authorize(Policy = Politicas.GestaoOp)]
        [HttpPut("{ordemProducao}/reabrir")]
        public async Task<IActionResult> ReabrirProducao(int ordemProducao)
        {
            var sucesso = await _producaoService.ReabrirProducaoAsync(ordemProducao, NomeUsuarioLogado());
            if (!sucesso)
                return NotFound($"OP {ordemProducao} não encontrada ou já está aberta.");
            return Ok(new { Message = $"Ordem de Produção {ordemProducao} reaberta. Consumo de A Granel estornado." });
        }

        // ===== NOVO ENDPOINT ADICIONADO =====
        [Authorize(Policy = Politicas.GestaoOp)]
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

        [Authorize(Policy = Politicas.Administracao)]
        [HttpDelete("{ordemProducao}")]
        public async Task<IActionResult> DeleteProducao(int ordemProducao)
        {
            var sucesso = await _producaoService.DeleteProducaoAsync(ordemProducao);
            if (!sucesso)
            {
                return NotFound($"OP {ordemProducao} não encontrada.");
            }
            return Ok(new { Message = $"Ordem de Produção {ordemProducao} e todos os seus registros atrelados foram excluídos com sucesso." });
        }
        // ===================================

        private string? NomeUsuarioLogado()
        {
            var nome = User.FindFirst("name")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            return string.IsNullOrWhiteSpace(nome) ? null : nome;
        }
    }
}