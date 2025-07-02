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
    public class PerdasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PerdasController> _logger;

        public PerdasController(AppDbContext context, ILogger<PerdasController> logger)
        {
            _context = context;
            _logger = logger;
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
    }
}
