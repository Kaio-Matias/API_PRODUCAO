using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EficienciaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EficienciaController> _logger;

        public EficienciaController(AppDbContext context, ILogger<EficienciaController> logger)
        {
            _context = context;
            _logger = logger;
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
    }
}
