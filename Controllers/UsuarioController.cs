using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using System.Threading.Tasks;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsuarioController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Usuario/login
        // Endpoint para validar um usuário existente pela matrícula.
        [HttpPost("login")]
        public async Task<ActionResult<Usuarios>> Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null || !loginRequest.Matricula.HasValue)
            {
                return BadRequest("O campo Matrícula é obrigatório.");
            }

            var usuario = await _context.Usuarios
                                        .FirstOrDefaultAsync(u => u.Matricula == loginRequest.Matricula.Value);

            if (usuario == null)
            {
                return NotFound("Usuário com esta matrícula não foi encontrado.");
            }

            return Ok(usuario);
        }

        // POST: api/Usuario
        // Endpoint para registrar um novo usuário.
        [HttpPost]
        public async Task<ActionResult<Usuarios>> PostUsuario([FromBody] Usuarios usuario)
        {
            // Validação dos dados recebidos
            if (usuario == null || !usuario.Matricula.HasValue || string.IsNullOrWhiteSpace(usuario.Nome))
            {
                return BadRequest("Nome e Matrícula são campos obrigatórios.");
            }

            // Verifica se já existe um usuário com a mesma matrícula para evitar duplicados
            var usuarioExistente = await _context.Usuarios
                                                 .AnyAsync(u => u.Matricula == usuario.Matricula);

            if (usuarioExistente)
            {
                return Conflict("Já existe um usuário cadastrado com esta matrícula."); // Retorna 409 Conflict
            }

            // Adiciona o novo usuário ao contexto do banco de dados
            _context.Usuarios.Add(usuario);
            // Salva as mudanças no banco de dados
            await _context.SaveChangesAsync();

            // Retorna uma resposta 201 Created com os dados do usuário criado
            // e um link para o recurso recém-criado no Header 'Location'.
            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuario);
        }

        // GET: api/Usuario/5
        // Endpoint auxiliar para o CreatedAtAction funcionar corretamente.
        // Retorna um usuário específico pelo seu Id.
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuarios>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            return Ok(usuario);
        }


        // DTO (Data Transfer Object) para o request de login
        public class LoginRequest
        {
            public int? Matricula { get; set; }
        }
    }
}
