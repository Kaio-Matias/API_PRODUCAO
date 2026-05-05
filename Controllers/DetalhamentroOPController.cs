using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Valedourado.Shared.Dtos;
using System.Linq;
using System.Collections.Generic;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DetalhamentoOPController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        // POST: api/DetalhamentoOP
        [HttpPost]
        public async Task<ActionResult<DetalhamentoOP>> PostDetalhamento([FromBody] DetalhamentoOP detalhamento)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.DetalhamentoOPs.Add(detalhamento);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(PostDetalhamento), new { id = detalhamento.Id }, detalhamento);
        }

        // GET: api/DetalhamentoOP/op/1143
        [HttpGet("op/{ordemProducao}")]
        public async Task<ActionResult<IEnumerable<DetalhamentoOP>>> GetDetalhamentosPorOp(int ordemProducao)
        {
            var registros = await _context.DetalhamentoOPs
                .Where(d => d.OrdemProducao == ordemProducao)
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return Ok(registros);
        }

        // PUT: api/DetalhamentoOP/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDetalhamento(int id, [FromBody] UpdateDetalhamentoOpDto dto)
        {
            if (id != dto.Id) return BadRequest("ID inconsistente.");

            var existing = await _context.DetalhamentoOPs
                .Include(d => d.Producao)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (existing == null) return NotFound();
            if (existing.Producao?.Status != "Aberto") return BadRequest("Não é possível editar lançamentos de uma OP fechada.");

            existing.Turno = dto.Turno ?? existing.Turno;
            existing.EmbProcessadas = dto.EmbProcessadas;
            existing.EmbProduzidas = dto.EmbProduzidas;
            existing.EmbPerdidas = dto.EmbPerdidas;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/DetalhamentoOP/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDetalhamento(int id)
        {
            var detalhamento = await _context.DetalhamentoOPs
                .Include(d => d.Producao)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (detalhamento == null) return NotFound();
            if (detalhamento.Producao?.Status != "Aberto") return BadRequest("Não é possível excluir lançamentos de uma OP fechada.");

            _context.DetalhamentoOPs.Remove(detalhamento);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
