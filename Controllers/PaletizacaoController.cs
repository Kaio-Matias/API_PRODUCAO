using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Services.Interfaces;
using API_PRODUCAO.Models;
using Valedourado.Shared.Dtos;
using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaletizacaoController : ControllerBase
    {
        private readonly IPaletizacaoService _paletizacaoService;
        private readonly IMapper _mapper;

        public PaletizacaoController(IPaletizacaoService paletizacaoService, IMapper mapper)
        {
            _paletizacaoService = paletizacaoService;
            _mapper = mapper;
        }

        [HttpGet("op/{ordemProducao}")]
        public async Task<ActionResult<IEnumerable<PaleteDto>>> GetPaletesPorOP(int ordemProducao)
        {
            var paletes = await _paletizacaoService.GetPaletesByOpAsync(ordemProducao);
            if (paletes == null || !paletes.Any())
            {
                return NotFound($"Nenhum palete encontrado para a OP: {ordemProducao}");
            }
            var resultadoDto = _mapper.Map<IEnumerable<PaleteDto>>(paletes);
            return Ok(resultadoDto);
        }

        [HttpPost]
        public async Task<ActionResult<PaleteDto>> CreatePalete([FromBody] CreatePaleteDto createPaleteDto)
        {
            if (createPaleteDto == null)
            {
                return BadRequest("Dados do palete inválidos.");
            }

            // 1. O Controller recebe o DTO e o passa DIRETAMENTE para o serviço.
            var paletizacaoEntity = await _paletizacaoService.CreatePaleteAsync(createPaleteDto);

            // 2. O Controller recebe a Entidade completa que o serviço salvou no banco.
            // 3. O Controller usa o AutoMapper para converter a Entidade em um DTO de resposta.
            var paleteResultDto = _mapper.Map<PaleteDto>(paletizacaoEntity);

            // 4. O Controller retorna o DTO de resposta para o cliente.
            return Ok(paleteResultDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQtdePalete(int id, [FromBody] UpdateQtdePaleteDto updateDto)
        {
            if (updateDto == null) return BadRequest("Dados de atualização inválidos.");

            var paleteAtualizado = await _paletizacaoService.UpdateQtdePaleteAsync(id, updateDto);

            if (paleteAtualizado == null) return NotFound($"Palete com ID {id} não encontrado.");

            return Ok(paleteAtualizado);
        }
    }
}