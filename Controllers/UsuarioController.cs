using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Services.Interfaces;
using API_PRODUCAO.DTOs;
using API_PRODUCAO.Models;
using AutoMapper;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IMapper _mapper;

        public UsuarioController(IUsuarioService usuarioService, IMapper mapper)
        {
            _usuarioService = usuarioService;
            _mapper = mapper;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UsuarioDto>> Login([FromBody] LoginRequestDto loginRequest)
        {
            var usuario = await _usuarioService.LoginAsync(loginRequest.Matricula.Value);
            if (usuario == null)
            {
                return NotFound("Usuário com esta matrícula não foi encontrado.");
            }
            var usuarioDto = _mapper.Map<UsuarioDto>(usuario);
            return Ok(usuarioDto);
        }

        [HttpPost("register")]
        public async Task<ActionResult<UsuarioDto>> Register([FromBody] CreateUsuarioDto usuarioDto)
        {
            if (await _usuarioService.UsuarioExistsAsync(usuarioDto.Matricula.Value))
            {
                return Conflict("Já existe um usuário cadastrado com esta matrícula.");
            }

            var usuario = _mapper.Map<Usuarios>(usuarioDto);
            var usuarioCriado = await _usuarioService.RegisterAsync(usuario);
            var resultadoDto = _mapper.Map<UsuarioDto>(usuarioCriado);

            // A rota para buscar um usuário por ID pode não existir, então retornamos apenas o objeto criado.
            return Created(nameof(Register), resultadoDto);
        }
    }
}