using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Services.Interfaces;
using Valedourado.Shared.Dtos; 
using AutoMapper;
using API_PRODUCAO.Models; 

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CadastroController(ICadastroService cadastroService, IMapper mapper) : ControllerBase
    {
        private readonly ICadastroService _cadastroService = cadastroService;
        private readonly IMapper _mapper = mapper;

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

        // POST: api/Cadastro
        [HttpPost]
        public async Task<ActionResult<CadastroDto>> PostCadastro([FromBody] CadastroDto cadastroDto)
        {
            var cadastro = _mapper.Map<Cadastro>(cadastroDto);
            var created = await _cadastroService.CreateAsync(cadastro);
            var resultDto = _mapper.Map<CadastroDto>(created);
            return CreatedAtAction(nameof(GetCadastros), new { id = resultDto.Id }, resultDto);
        }

        // PUT: api/Cadastro/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCadastro(int id, [FromBody] CadastroDto cadastroDto)
        {
            var cadastro = _mapper.Map<Cadastro>(cadastroDto);
            var updated = await _cadastroService.UpdateAsync(id, cadastro);
            if (updated == null) return NotFound();
            return NoContent();
        }

        // DELETE: api/Cadastro/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCadastro(int id)
        {
            var success = await _cadastroService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}