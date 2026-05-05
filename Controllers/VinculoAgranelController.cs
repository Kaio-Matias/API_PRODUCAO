using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using Microsoft.EntityFrameworkCore;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VinculoAgranelController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VinculoAgranelController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VinculoAgranelAcabado>>> GetVinculos()
        {
            return await _context.VinculosAgranelAcabado.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<VinculoAgranelAcabado>> PostVinculo(VinculoAgranelAcabado vinculo)
        {
            _context.VinculosAgranelAcabado.Add(vinculo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetVinculos), new { id = vinculo.Id }, vinculo);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVinculo(int id)
        {
            var vinculo = await _context.VinculosAgranelAcabado.FindAsync(id);
            if (vinculo == null) return NotFound();

            _context.VinculosAgranelAcabado.Remove(vinculo);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
