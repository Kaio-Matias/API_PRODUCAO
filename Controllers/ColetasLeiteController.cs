using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Valedourado.Shared.Dtos;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ColetasLeiteController : ControllerBase
    {
        private readonly IColetaLeiteService _service;
        private readonly API_PRODUCAO.Services.ColetaLeiteErpService _erp;

        public ColetasLeiteController(IColetaLeiteService service, API_PRODUCAO.Services.ColetaLeiteErpService erp)
        {
            _service = service;
            _erp = erp;
        }

        // ── Integração com o banco do sistema do leite ──────────────────────

        /// <summary>Situação da integração: configurada? sincronização automática? último resultado.</summary>
        [HttpGet("erp/status")]
        public ActionResult GetStatusErp() => Ok(new
        {
            configurado = _erp.Configurado,
            fonte = _erp.Fonte,
            sincronizacaoAutomatica = _erp.Configurado && _erp.Opcoes.SincronizacaoAutomatica,
            intervaloMinutos = Math.Max(5, _erp.Opcoes.IntervaloMinutos),
            mesesCobertos = Math.Clamp(_erp.Opcoes.MesesAnteriores, 0, 12) + 1,
            ultima = API_PRODUCAO.Services.ColetaLeiteErpService.UltimaSincronizacao,
        });

        /// <summary>
        /// Pré-visualização: só LÊ o sistema do leite e mostra o que seria importado (não grava nada).
        /// Use para conferir a consulta configurada antes de ligar a sincronização.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = API_PRODUCAO.Security.Politicas.Administracao)]
        [HttpGet("erp/previa")]
        public async Task<ActionResult> PreviaErp([FromQuery] int mes, [FromQuery] int ano, CancellationToken ct)
        {
            if (!_erp.Configurado)
                return BadRequest(new { message = "Integração com o banco do leite ainda não configurada (appsettings → LeiteErp: Servidor, Banco, Usuario, Senha)." });
            if (mes is < 1 or > 12 || ano < 2000) return BadRequest(new { message = "Informe mês e ano válidos." });

            var inicio = new DateTime(ano, mes, 1);
            try
            {
                var lidas = await _erp.BuscarAsync(inicio, inicio.AddMonths(1).AddTicks(-1), ct);
                return Ok(new { total = lidas.Count, amostra = lidas.OrderByDescending(c => c.DataHoraColeta).Take(50) });
            }
            catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException or InvalidCastException or FormatException)
            {
                return StatusCode(502, new { message = $"Falha ao ler o sistema do leite: {ex.Message}" });
            }
        }

        /// <summary>Sincroniza agora o mês informado (ou o período padrão) a partir do sistema do leite.</summary>
        [HttpPost("erp/sincronizar")]
        public async Task<ActionResult> SincronizarErp([FromQuery] int? mes, [FromQuery] int? ano, CancellationToken ct)
        {
            if (!_erp.Configurado)
                return BadRequest(new { message = "Integração com o banco do leite ainda não configurada (appsettings → LeiteErp)." });

            DateTime inicio, fim;
            if (mes is >= 1 and <= 12 && ano is > 2000)
            {
                inicio = new DateTime(ano.Value, mes.Value, 1);
                fim = inicio.AddMonths(1).AddTicks(-1);
            }
            else
            {
                (inicio, fim) = API_PRODUCAO.Services.ColetaLeiteErpService.PeriodoPadrao(DateTime.Now, _erp.Opcoes.MesesAnteriores);
            }

            var r = await _erp.SincronizarAsync(inicio, fim, ct);
            return r.Erro is null ? Ok(r) : StatusCode(502, new { message = $"Falha ao ler o sistema do leite: {r.Erro}", resultado = r });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ColetaLeiteDto>>> GetByMesAno([FromQuery] int mes, [FromQuery] int ano)
        {
            var items = await _service.GetByMesAnoAsync(mes, ano);
            return Ok(items.Select(ToDto));
        }

        [HttpPost]
        public async Task<ActionResult<ColetaLeiteDto>> Create([FromBody] CreateColetaLeiteDto dto)
        {
            var numero = dto.NumeroColeta.Trim();
            if (await _service.NumeroExistsAsync(numero))
                return Conflict(new { message = "Já existe uma coleta com esse número." });

            var result = await _service.CreateAsync(ToEntity(dto, numero));
            return Ok(ToDto(result));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ColetaLeiteDto>> Update(int id, [FromBody] CreateColetaLeiteDto dto)
        {
            var numero = dto.NumeroColeta.Trim();
            if (await _service.NumeroExistsAsync(numero, id))
                return Conflict(new { message = "Já existe uma coleta com esse número." });

            var result = await _service.UpdateAsync(id, ToEntity(dto, numero));
            if (result == null) return NotFound();
            return Ok(ToDto(result));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        private static ColetaLeite ToEntity(CreateColetaLeiteDto dto, string numero) => new()
        {
            NumeroColeta = numero,
            DataHoraColeta = dto.DataHoraColeta,
            PlacaCaminhao = dto.PlacaCaminhao.Trim(),
            Transportador = dto.Transportador.Trim(),
            Linha = dto.Linha.Trim(),
            Produtor = dto.Produtor?.Trim() ?? string.Empty,
            VolumeDescarregado = dto.VolumeDescarregado,
            VolumeDetalhado = dto.VolumeDetalhado
        };

        private static ColetaLeiteDto ToDto(ColetaLeite coleta)
        {
            var diferenca = coleta.VolumeDetalhado - coleta.VolumeDescarregado;
            return new ColetaLeiteDto
            {
                Id = coleta.Id,
                NumeroColeta = coleta.NumeroColeta,
                DataHoraColeta = coleta.DataHoraColeta,
                PlacaCaminhao = coleta.PlacaCaminhao,
                Transportador = coleta.Transportador,
                Linha = coleta.Linha,
                Produtor = coleta.Produtor,
                VolumeDescarregado = coleta.VolumeDescarregado,
                VolumeDetalhado = coleta.VolumeDetalhado,
                DiferencaLitros = diferenca,
                DiferencaPercentual = coleta.VolumeDescarregado == 0
                    ? null
                    : diferenca / coleta.VolumeDescarregado * 100
            };
        }
    }
}
