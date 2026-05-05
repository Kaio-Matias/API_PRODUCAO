using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DumpController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DumpController(AppDbContext context) { _context = context; }

        [HttpGet("paletes")]
        public async Task<IActionResult> GetPaletes()
        {
            var data = await _context.Paletizacoes
                .Include(p => p.Producao)
                .Where(p => p.DataHoraPaletizacao >= new System.DateTime(2026, 4, 1))
                .Select(p => new {
                    p.OrdemProducao,
                    p.CodigoProduto,
                    p.QtdeCx,
                    p.QtdePorPalete,
                    p.QtdeProduzida,
                    Status = p.Producao != null ? p.Producao.Status : "N/A",
                    Data = p.DataHoraPaletizacao
                })
                .Take(50)
                .ToListAsync();
            return Ok(data);
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var status = await _context.Producoes
                .Select(o => o.Status)
                .Distinct()
                .ToListAsync();
            return Ok(status);
        }
    }
}
