using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProducoesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProducoesController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Producoes
        [HttpPost]
        public async Task<ActionResult<Producoes>> PostProducao([FromBody] Producoes producao)
        {
            // Validação para garantir que os campos essenciais foram enviados
            if (!ModelState.IsValid || string.IsNullOrEmpty(producao.Produto) || string.IsNullOrEmpty(producao.Unidade))
            {
                return BadRequest("Modelo de dados inválido. Produto e Unidade são obrigatórios.");
            }

            // Garante que o status e a data de abertura sejam definidos no servidor
            producao.Status = "Aberto";
            producao.DataHoraAbertura = DateTime.Now;
            producao.DataHoraFechamento = null; // Garante que o fechamento seja nulo na criação

            _context.Producoes.Add(producao);
            await _context.SaveChangesAsync();

            // Retorna o objeto criado, incluindo a nova 'OrdemProducao' gerada pelo banco
            return CreatedAtAction(nameof(GetProducao), new { id = producao.OrdemProducao }, producao);
        }

        // GET: api/Producoes/5 (Endpoint auxiliar para o CreatedAtAction)
        [HttpGet("{id}")]
        public async Task<ActionResult<Producoes>> GetProducao(int id)
        {
            var producao = await _context.Producoes.FindAsync(id);

            if (producao == null)
            {
                return NotFound();
            }

            return producao;
        }

        // GET: api/Producoes (Adicionado para consulta)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producoes>>> GetProducoes()
        {
            return await _context.Producoes.ToListAsync();
        }
    }
}
