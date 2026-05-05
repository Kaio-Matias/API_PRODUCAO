using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Valedourado.Shared.Dtos;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetaProducaoController : ControllerBase
    {
        private readonly IMetaProducaoService _service;

        public MetaProducaoController(IMetaProducaoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MetaProducaoDto>>> GetMetas([FromQuery] int mes, [FromQuery] int ano)
        {
            var metas = await _service.GetMetasByMesAnoAsync(mes, ano);
            var dtos = metas.Select(m => new MetaProducaoDto
            {
                Id = m.Id, Mes = m.Mes, Ano = m.Ano, Classe = m.Classe,
                CodProduto = m.CodProduto,
                MetaCaixas = m.MetaCaixas, MetaKg = m.MetaKg, MetaRS = m.MetaRS
            });
            return Ok(dtos);
        }

        [HttpPost]
        public async Task<ActionResult<MetaProducaoDto>> UpsertMeta([FromBody] CreateMetaProducaoDto dto)
        {
            var meta = new MetaProducao
            {
                Mes = dto.Mes, Ano = dto.Ano, Classe = dto.Classe,
                CodProduto = string.IsNullOrWhiteSpace(dto.CodProduto) ? null : dto.CodProduto,
                MetaCaixas = dto.MetaCaixas, MetaKg = dto.MetaKg, MetaRS = dto.MetaRS
            };
            var result = await _service.UpsertMetaAsync(meta);
            var resultDto = new MetaProducaoDto
            {
                Id = result.Id, Mes = result.Mes, Ano = result.Ano, Classe = result.Classe,
                CodProduto = result.CodProduto,
                MetaCaixas = result.MetaCaixas, MetaKg = result.MetaKg, MetaRS = result.MetaRS
            };
            return Ok(resultDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeta(int id)
        {
            var ok = await _service.DeleteMetaAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        // --- Meta Captação de Leite ---

        [HttpGet("captacao")]
        public async Task<ActionResult<MetaCaptacaoLeiteDto>> GetMetaCaptacao([FromQuery] int mes, [FromQuery] int ano)
        {
            var meta = await _service.GetMetaCaptacaoAsync(mes, ano);
            if (meta == null)
                return Ok(new MetaCaptacaoLeiteDto { Mes = mes, Ano = ano, MetaLitros = 0 });

            return Ok(new MetaCaptacaoLeiteDto { Id = meta.Id, Mes = meta.Mes, Ano = meta.Ano, MetaLitros = meta.MetaLitros });
        }

        [HttpPost("captacao")]
        public async Task<ActionResult<MetaCaptacaoLeiteDto>> UpsertMetaCaptacao([FromBody] CreateMetaCaptacaoLeiteDto dto)
        {
            var meta = new MetaCaptacaoLeite { Mes = dto.Mes, Ano = dto.Ano, MetaLitros = dto.MetaLitros };
            var result = await _service.UpsertMetaCaptacaoAsync(meta);
            return Ok(new MetaCaptacaoLeiteDto { Id = result.Id, Mes = result.Mes, Ano = result.Ano, MetaLitros = result.MetaLitros });
        }
    }
}
