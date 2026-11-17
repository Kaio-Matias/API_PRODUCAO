using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using API_PRODUCAO.Services.Interfaces;
using API_PRODUCAO.Models;
using AutoMapper;
using Valedourado.Shared.Dtos;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API_PRODUCAO.Security;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;

        public UsuarioController(IUsuarioService usuarioService, IMapper mapper, IConfiguration config)
        {
            _usuarioService = usuarioService;
            _mapper         = mapper;
            _config         = config;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginRequest)
        {
            var usuario = await _usuarioService.LoginAsync(loginRequest.Matricula!.Value);
            if (usuario == null)
                return NotFound("Usuário com esta matrícula não foi encontrado.");

            var usuarioDto = _mapper.Map<UsuarioDto>(usuario);
            var (token, expiresAt) = GerarJwt(usuarioDto);

            return Ok(new LoginResponseDto
            {
                Token     = token,
                ExpiresAt = expiresAt,
                Usuario   = usuarioDto,
            });
        }

        // Somente Supervisor/Admin cadastram usuários; somente Admin cria outro Admin.
        [Authorize(Policy = Politicas.Administracao)]
        [HttpPost("register")]
        public async Task<ActionResult<UsuarioDto>> Register([FromBody] CreateUsuarioDto usuarioDto)
        {
            var cargo = Cargos.Normalizar(usuarioDto.Cargo);
            if (!string.IsNullOrWhiteSpace(usuarioDto.Cargo) &&
                !Cargos.Todos.Contains(usuarioDto.Cargo.Trim(), StringComparer.OrdinalIgnoreCase))
                return BadRequest($"Cargo inválido. Use: {string.Join(", ", Cargos.Todos)}.");

            if (cargo == Cargos.Admin && !User.IsInRole(Cargos.Admin))
                return StatusCode(StatusCodes.Status403Forbidden, "Somente um Admin pode cadastrar outro Admin.");

            if (await _usuarioService.UsuarioExistsAsync(usuarioDto.Matricula!.Value))
                return Conflict("Já existe um usuário cadastrado com esta matrícula.");

            var usuario        = _mapper.Map<Usuarios>(usuarioDto);
            usuario.Cargo      = cargo;
            var usuarioCriado  = await _usuarioService.RegisterAsync(usuario);
            var resultadoDto   = _mapper.Map<UsuarioDto>(usuarioCriado);

            return Created(nameof(Register), resultadoDto);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetAllUsuarios()
        {
            var usuarios    = await _usuarioService.GetTodosUsuariosAsync();
            var usuariosDto = _mapper.Map<IEnumerable<UsuarioDto>>(usuarios);
            return Ok(usuariosDto);
        }

        [Authorize(Policy = Politicas.Administracao)]
        [HttpPut("{id}/modulos")]
        public async Task<ActionResult<UsuarioDto>> UpdateModulosAcesso(int id, [FromBody] UpdateUsuarioModulosDto modulosDto)
        {
            var jsonModulos = System.Text.Json.JsonSerializer.Serialize(modulosDto.Modulos_Acesso);

            var usuarioDb = await _usuarioService.UpdateModulosAcessoAsync(id, jsonModulos);
            if (usuarioDb == null)
                return NotFound("Usuário não encontrado.");

            var usuarioDto = _mapper.Map<UsuarioDto>(usuarioDb);
            return Ok(usuarioDto);
        }

        [Authorize(Policy = Politicas.Administracao)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            if (User.FindFirst("id")?.Value == id.ToString())
                return BadRequest("Você não pode excluir o próprio usuário.");

            var deleted = await _usuarioService.DeleteAsync(id);
            if (!deleted) return NotFound("Usuário não encontrado.");
            return NoContent();
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private (string Token, DateTime ExpiresAt) GerarJwt(UsuarioDto usuario)
        {
            var key        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds      = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryHours = int.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h : 12;
            var expiresAt  = DateTime.Now.AddHours(expiryHours);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,  usuario.Matricula.ToString()!),
                new Claim(JwtRegisteredClaimNames.Name, usuario.Nome ?? ""),
                // Normalizado para casar com [Authorize]/IsInRole, que diferenciam maiúsculas
                new Claim(ClaimTypes.Role,              Cargos.Normalizar(usuario.Cargo)),
                new Claim("id",                         usuario.Id.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer:             _config["Jwt:Issuer"],
                audience:           _config["Jwt:Audience"],
                claims:             claims,
                expires:            expiresAt,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
