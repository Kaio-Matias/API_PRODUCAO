using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CadastroController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CadastroController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Cadastro
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cadastro>>> GetCadastros()
        {
            var cadastros = await _context.Cadastros.ToListAsync();
            return Ok(cadastros);
        }
    }
}