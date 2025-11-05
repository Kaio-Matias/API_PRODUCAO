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
    public class EficienciaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EficienciaController> _logger;
        private readonly IMapper _mapper; // MAPPER ADICIONADO

        // Construtor atualizado para injetar o IMapper
        public EficienciaController(AppDbContext context, ILogger<EficienciaController> logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper; // MAPPER ATRIBUÍDO
        }

        /// <summary>
        /// Recebe e salva uma lista de registos de tempo de eficiência.
        /// </summary>
        /// <param name="registos">Uma lista de objetos de Eficiencia.</param>
        /// <returns>Uma resposta de sucesso ou um erro.</returns>
        [HttpPost]
        public async Task<IActionResult> PostEficiencia([FromBody] List<Eficiencia> registos)
        {
            if (registos == null || !registos.Any())
            {
                return BadRequest("A lista de registos de eficiência não pode estar vazia.");
            }

            // --- CORREÇÃO AQUI ---
            // Compara o tempo com TimeSpan.Zero em vez de 0.
            if (registos.Any(r => r.OrdemProducao <= 0 || string.IsNullOrEmpty(r.Motivo) || r.Tempo < TimeSpan.Zero))
            {
                return BadRequest("Um ou mais registos de eficiência contêm dados inválidos.");
            }

            try
            {
                // Adiciona a data/hora do registro antes de salvar
                var dataRegistro = DateTime.Now;
                foreach (var registro in registos)
                {
                    registro.DataRegistro = dataRegistro;
                }

                await _context.Eficiencia.AddRangeAsync(registos);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"{registos.Count} registo(s) de eficiência foram salvos com sucesso para a OP: {registos.First().OrdemProducao}.");
                return Ok(new { Message = $"{registos.Count} registo(s) de eficiência foram salvos com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao salvar os registos de eficiência.");
                return StatusCode(500, "Ocorreu um erro interno no servidor ao salvar os registos de eficiência.");
            }
        }

        // ===== NOVO ENDPOINT GET ADICIONADO =====
        /// <summary>
        /// Busca todos os registros de eficiência para uma OP específica e um operador.
        /// </summary>
        [HttpGet("op/{ordemProducao}/operador/{operador}")]
        [ProducesResponseType(typeof(IEnumerable<EficienciaDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<EficienciaDto>>> GetEficienciaPorOpEOperador(int ordemProducao, string operador)
        {
            if (string.IsNullOrEmpty(operador))
            {
                return BadRequest("O nome do operador é obrigatório.");
            }

            try
            {
                var registros = await _context.Eficiencia
                    .Where(e => e.OrdemProducao == ordemProducao && e.Operador == operador)
                    .OrderByDescending(e => e.DataRegistro) // Ordena pelos mais recentes
                    .ToListAsync();

                if (registros == null || !registros.Any())
                {
                    // Retorna 404 Not Found se nenhum registro corresponder
                    return NotFound("Nenhum registro encontrado para esta OP e operador.");
                }

                // Mapeia a entidade 'Eficiencia' para 'EficienciaDto'
                var registrosDto = _mapper.Map<IEnumerable<EficienciaDto>>(registros);

                return Ok(registrosDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar registros de eficiência por OP e operador.");
                return StatusCode(500, "Ocorreu um erro interno no servidor.");
            }
        }
    }
}