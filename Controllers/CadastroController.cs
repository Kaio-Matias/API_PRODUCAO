using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Services.Interfaces;
using Valedourado.Shared.Dtos; // <--- LINHA CORRETA
using AutoMapper;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CadastroController : ControllerBase
    {
        private readonly ICadastroService _cadastroService;
        private readonly IMapper _mapper;

        public CadastroController(ICadastroService cadastroService, IMapper mapper)
        {
            _cadastroService = cadastroService;
            _mapper = mapper;
        }

        // GET: api/Cadastro
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CadastroDto>>> GetCadastros()
        {
            var cadastros = await _cadastroService.GetAllAsync();
            var cadastrosDto = _mapper.Map<IEnumerable<CadastroDto>>(cadastros);
            return Ok(cadastrosDto);
        }

        // GET: api/Cadastro/produto/{codProduto}
        [HttpGet("produto/{codProduto}")]
        public async Task<ActionResult<CadastroDto>> GetCadastroPorCodProduto(string codProduto)
        {
            var cadastro = await _cadastroService.GetByCodProdutoAsync(codProduto);
            if (cadastro == null)
            {
                return NotFound($"Nenhum produto encontrado com o código: {codProduto}");
            }
            var cadastroDto = _mapper.Map<CadastroDto>(cadastro);
            return Ok(cadastroDto);
        }

        // GET: api/Cadastro/barcode/{codBarra}
        [HttpGet("barcode/{codBarra}")]
        public async Task<ActionResult<CadastroDto>> GetCadastroPorCodBarra(string codBarra)
        {
            var cadastro = await _cadastroService.GetByCodBarraAsync(codBarra);
            if (cadastro == null)
            {
                return NotFound($"Nenhum produto encontrado com o código de barras: {codBarra}");
            }
            // Mapeia a entidade para o DTO antes de enviar
            var cadastroDto = _mapper.Map<CadastroDto>(cadastro);
            return Ok(cadastroDto);
        }
    }
}