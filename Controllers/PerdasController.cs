using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; // USING ADICIONADO
using AutoMapper; // USING ADICIONADO
using Valedourado.Shared.Dtos; // USING ADICIONADO

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PerdasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PerdasController> _logger;
        private readonly IMapper _mapper; // MAPPER ADICIONADO

        // Construtor atualizado para injetar o IMapper
        public PerdasController(AppDbContext context, ILogger<PerdasController> logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper; // MAPPER ATRIBUÍDO
        }

        /// <summary>
        /// Recebe e salva uma lista de registos de perdas para uma Ordem de Produção.
        /// </summary>
        /// <param name="perdas">Uma lista de objetos de Perda.</param>
        /// <returns>Uma resposta de sucesso ou um erro.</returns>
        [HttpPost]
        public async Task<IActionResult> PostPerdas([FromBody] List<Perdas> perdas)
        {
            if (perdas == null || !perdas.Any())
            {
                return BadRequest("A lista de perdas não pode estar vazia.");
            }

            if (perdas.Any(p => p.OrdemProducao <= 0 || string.IsNullOrEmpty(p.Motivo)))
            {
                return BadRequest("Um ou mais registos de perda contêm dados inválidos.");
            }

            try
            {
                // Adiciona a data/hora do registro antes de salvar
                var dataRegistro = DateTime.Now;
                foreach (var perda in perdas)
                {
                    perda.DataRegistro = dataRegistro;
                }

                await _context.Perdas.AddRangeAsync(perdas);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"{perdas.Count} registo(s) de perda foram salvos com sucesso para a OP: {perdas.First().OrdemProducao}.");
                return Ok(new { Message = $"{perdas.Count} registo(s) de perda foram salvos com sucesso." });
            }
            catch (Exception ex)
            {
                // Log do erro detalhado no servidor para depuração.
                _logger.LogError(ex, "Ocorreu um erro ao salvar os registos de perdas.");
                // Retorna uma mensagem de erro 500 genérica para o cliente.
                return StatusCode(500, "Ocorreu um erro interno no servidor. Por favor, tente novamente mais tarde.");
            }
        }

        // GET: api/Perdas/op/1143 (todas as perdas de uma OP, sem filtro por operador)
        [HttpGet("op/{ordemProducao}")]
        public async Task<ActionResult<IEnumerable<PerdasDto>>> GetPerdasPorOp(int ordemProducao)
        {
            try
            {
                var registros = await _context.Perdas
                    .Where(p => p.OrdemProducao == ordemProducao)
                    .OrderByDescending(p => p.DataRegistro)
                    .ToListAsync();

                var registrosDto = _mapper.Map<IEnumerable<PerdasDto>>(registros);
                return Ok(registrosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar registros de perdas por OP.");
                return StatusCode(500, "Ocorreu um erro interno no servidor.");
            }
        }

        // ===== ENDPOINT GET POR OP + OPERADOR =====
        /// <summary>
        /// Busca todos os registros de perdas para uma OP específica e um operador.
        /// </summary>
        [HttpGet("op/{ordemProducao}/operador/{operador}")]
        [ProducesResponseType(typeof(IEnumerable<PerdasDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<PerdasDto>>> GetPerdasPorOpEOperador(int ordemProducao, string operador)
        {
            if (string.IsNullOrEmpty(operador))
            {
                return BadRequest("O nome do operador é obrigatório.");
            }

            try
            {
                var registros = await _context.Perdas
                    .Where(p => p.OrdemProducao == ordemProducao && p.Operador == operador)
                    .OrderByDescending(p => p.DataRegistro) // Ordena pelos mais recentes
                    .ToListAsync();

                if (registros == null || !registros.Any())
                {
                    // Retorna 404 Not Found se nenhum registro corresponder
                    return NotFound("Nenhum registro encontrado para esta OP e operador.");
                }

                // Mapeia a entidade 'Perdas' para 'PerdasDto' para retornar ao cliente
                // (Assumindo que você tem um DTO 'PerdasDto' e o mapeamento configurado)
                var registrosDto = _mapper.Map<IEnumerable<PerdasDto>>(registros);

                return Ok(registrosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar registros de perdas por OP e operador.");
                return StatusCode(500, "Ocorreu um erro interno no servidor.");
            }
        }
        // PUT: api/Perdas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPerda(int id, [FromBody] PerdasDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != dto.Id) return BadRequest("ID inconsistente.");

            var existing = await _context.Perdas
                .Include(p => p.Producao)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existing == null) return NotFound();
            if (existing.Producao?.Status != "Aberto") return BadRequest("Não é possível editar lançamentos de uma OP fechada.");

            existing.Motivo = dto.Motivo;
            existing.Quantidade = dto.Quantidade;
            existing.Operador = dto.Operador; 
            
            if (dto.OrdemProducao.HasValue)
            {
                existing.OrdemProducao = dto.OrdemProducao.Value;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Perdas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePerda(int id)
        {
            var perda = await _context.Perdas
                .Include(p => p.Producao)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (perda == null) return NotFound();
            if (perda.Producao?.Status != "Aberto") return BadRequest("Não é possível excluir lançamentos de uma OP fechada.");

            _context.Perdas.Remove(perda);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}