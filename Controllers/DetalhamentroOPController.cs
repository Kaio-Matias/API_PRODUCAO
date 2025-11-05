using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using System.Threading.Tasks;

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
    }
}
