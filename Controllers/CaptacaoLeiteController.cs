using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Valedourado.Shared.Dtos;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaptacaoLeiteController : ControllerBase
    {
        private readonly ICaptacaoLeiteService _service;

        public CaptacaoLeiteController(ICaptacaoLeiteService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CaptacaoLeiteDto>>> GetByMesAno([FromQuery] int mes, [FromQuery] int ano)
        {
            var items = await _service.GetByMesAnoAsync(mes, ano);
            var dtos = items.Select(c => new CaptacaoLeiteDto
            {
                Id = c.Id,
                DataRecebimento = c.DataRecebimento,
                VolumeRecebido = c.VolumeRecebido,
                Observacao = c.Observacao
            });
            return Ok(dtos);
        }

        [HttpPost]
        public async Task<ActionResult<CaptacaoLeiteDto>> Create([FromBody] CreateCaptacaoLeiteDto dto)
        {
            var captacao = new CaptacaoLeite
            {
                DataRecebimento = dto.DataRecebimento,
                VolumeRecebido = dto.VolumeRecebido,
                Observacao = dto.Observacao
            };
            var result = await _service.CreateAsync(captacao);
            return Ok(new CaptacaoLeiteDto
            {
                Id = result.Id,
                DataRecebimento = result.DataRecebimento,
                VolumeRecebido = result.VolumeRecebido,
                Observacao = result.Observacao
            });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CaptacaoLeiteDto>> Update(int id, [FromBody] CreateCaptacaoLeiteDto dto)
        {
            var captacao = new CaptacaoLeite
            {
                DataRecebimento = dto.DataRecebimento,
                VolumeRecebido = dto.VolumeRecebido,
                Observacao = dto.Observacao
            };
            var result = await _service.UpdateAsync(id, captacao);
            if (result == null) return NotFound();
            return Ok(new CaptacaoLeiteDto
            {
                Id = result.Id,
                DataRecebimento = result.DataRecebimento,
                VolumeRecebido = result.VolumeRecebido,
                Observacao = result.Observacao
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
