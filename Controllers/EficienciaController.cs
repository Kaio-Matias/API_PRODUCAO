using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Valedourado.Shared.Dtos;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EficienciaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EficienciaController> _logger;
        private readonly IMapper _mapper;

        public EficienciaController(AppDbContext context, ILogger<EficienciaController> logger, IMapper mapper)
        {
            _context = context;
            _logger  = logger;
            _mapper  = mapper;
        }

        // ── POST /api/Eficiencia ───────────────────────────────────
        /// <summary>
        /// Inicia uma nova parada. Captura DataHoraInicio = agora no servidor.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<EficienciaDto>> IniciarParada([FromBody] CreateEficienciaDto dto)
        {
            if (dto == null || dto.OrdemProducao <= 0 || string.IsNullOrWhiteSpace(dto.Motivo))
                return BadRequest("OrdemProducao e Motivo são obrigatórios.");

            var op = await _context.Producoes.FirstOrDefaultAsync(p => p.OrdemProducao == dto.OrdemProducao);
            if (op == null)  return NotFound("Ordem de Produção não encontrada.");
            if (op.Status != "Aberto") return BadRequest("Não é possível registrar paradas em uma OP que não está aberta.");

            // Modo manual: valida que fim > início quando ambos são fornecidos
            if (dto.DataHoraInicio.HasValue && dto.DataHoraFim.HasValue
                && dto.DataHoraFim.Value <= dto.DataHoraInicio.Value)
                return BadRequest("A hora de fim deve ser posterior à hora de início.");
             
            var inicio = dto.DataHoraInicio ?? DateTime.Now;
            var fim    = dto.DataHoraFim;

            var parada = new Eficiencia
            {
                OrdemProducao  = dto.OrdemProducao,
                Motivo         = dto.Motivo,
                Operador       = dto.Operador,
                DataHoraInicio = inicio,
                DataHoraFim    = fim,
                Tempo          = fim.HasValue ? fim.Value - inicio : null,
                DataRegistro   = DateTime.Now,
            };

            _context.Eficiencia.Add(parada);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Parada iniciada — OP {Op}, Motivo: {Motivo}, Início: {Inicio}",
                dto.OrdemProducao, dto.Motivo, parada.DataHoraInicio);

            return CreatedAtAction(nameof(GetParadaPorId), new { id = parada.Id }, MapToDto(parada));
        }

        // ── PUT /api/Eficiencia/{id}/finalizar ─────────────────────
        /// <summary>
        /// Finaliza uma parada em andamento. Captura DataHoraFim = agora e calcula Tempo.
        /// Usa UPDATE condicional (WHERE DataHoraFim IS NULL) para evitar dupla finalização concorrente.
        /// </summary>
        [HttpPut("{id}/finalizar")]
        public async Task<ActionResult<EficienciaDto>> FinalizarParada(int id)
        {
            var fim = DateTime.Now;

            // Atualização atômica: só age se DataHoraFim ainda for null no banco.
            var affected = await _context.Eficiencia
                .Where(e => e.Id == id && e.DataHoraFim == null)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.DataHoraFim, fim));

            if (affected == 0)
            {
                var existe = await _context.Eficiencia.AnyAsync(e => e.Id == id);
                return existe
                    ? BadRequest("Esta parada já foi finalizada.")
                    : NotFound("Parada não encontrada.");
            }

            // Recarrega para calcular Tempo e validar status da OP.
            var parada = await _context.Eficiencia
                .Include(e => e.Producao)
                .FirstAsync(e => e.Id == id);

            if (parada.Producao?.Status != "Aberto")
                return BadRequest("A OP desta parada não está mais aberta.");

            parada.Tempo = fim - parada.DataHoraInicio;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Parada finalizada — ID {Id}, Duração: {Dur}", id, parada.Tempo);

            return Ok(MapToDto(parada));
        }

        // ── GET /api/Eficiencia/op/{op} ───────────────────────────
        /// <summary>Todas as paradas (ativas + concluídas) de uma OP.</summary>
        [HttpGet("op/{ordemProducao}")]
        public async Task<ActionResult<IEnumerable<EficienciaDto>>> GetParadasPorOp(int ordemProducao)
        {
            var lista = await _context.Eficiencia
                .Where(e => e.OrdemProducao == ordemProducao)
                .OrderByDescending(e => e.DataHoraInicio)
                .ToListAsync();

            return Ok(lista.Select(MapToDto));
        }

        // ── GET /api/Eficiencia/op/{op}/ativas ────────────────────
        /// <summary>Somente paradas em andamento (sem DataHoraFim) de uma OP.</summary>
        [HttpGet("op/{ordemProducao}/ativas")]
        public async Task<ActionResult<IEnumerable<EficienciaDto>>> GetParadasAtivas(int ordemProducao)
        {
            var lista = await _context.Eficiencia
                .Where(e => e.OrdemProducao == ordemProducao && e.DataHoraFim == null)
                .OrderBy(e => e.DataHoraInicio)
                .ToListAsync();

            return Ok(lista.Select(MapToDto));
        }

        // ── GET /api/Eficiencia/{id} ──────────────────────────────
        [HttpGet("{id}")]
        public async Task<ActionResult<EficienciaDto>> GetParadaPorId(int id)
        {
            var p = await _context.Eficiencia.FindAsync(id);
            if (p == null) return NotFound();
            return Ok(MapToDto(p));
        }

        // ── GET /api/Eficiencia/op/{op}/operador/{operador} ───────
        [HttpGet("op/{ordemProducao}/operador/{operador}")]
        public async Task<ActionResult<IEnumerable<EficienciaDto>>> GetPorOpEOperador(
            int ordemProducao, string operador)
        {
            if (string.IsNullOrEmpty(operador)) return BadRequest("Operador obrigatório.");

            var lista = await _context.Eficiencia
                .Where(e => e.OrdemProducao == ordemProducao && e.Operador == operador)
                .OrderByDescending(e => e.DataHoraInicio)
                .ToListAsync();

            if (!lista.Any()) return NotFound("Nenhum registro encontrado.");

            return Ok(lista.Select(MapToDto));
        }

        // ── PUT /api/Eficiencia/{id} (editar motivo/operador/timestamps) ──
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarParada(int id, [FromBody] UpdateEficienciaDto dto)
        {
            if (id != dto.Id) return BadRequest("ID inconsistente.");

            var parada = await _context.Eficiencia
                .Include(e => e.Producao)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (parada == null) return NotFound();
            if (parada.Producao?.Status != "Aberto")
                return BadRequest("Não é possível editar lançamentos de uma OP fechada.");

            parada.Motivo   = dto.Motivo   ?? parada.Motivo;
            parada.Operador = dto.Operador ?? parada.Operador;

            if (dto.DataHoraInicio.HasValue)
                parada.DataHoraInicio = dto.DataHoraInicio.Value;

            if (dto.DataHoraFim.HasValue)
                parada.DataHoraFim = dto.DataHoraFim.Value;

            // Recalcula Tempo sempre que ambos os extremos estiverem definidos
            if (parada.DataHoraFim.HasValue)
            {
                if (parada.DataHoraFim.Value <= parada.DataHoraInicio)
                    return BadRequest("A hora de fim deve ser posterior à hora de início.");
                parada.Tempo = parada.DataHoraFim.Value - parada.DataHoraInicio;
            }

            await _context.SaveChangesAsync();
            return Ok(MapToDto(parada));
        }

        // ── DELETE /api/Eficiencia/{id} ───────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletarParada(int id)
        {
            var parada = await _context.Eficiencia
                .Include(e => e.Producao)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (parada == null) return NotFound();
            if (parada.Producao?.Status != "Aberto")
                return BadRequest("Não é possível excluir lançamentos de uma OP fechada.");

            _context.Eficiencia.Remove(parada);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── Helper ────────────────────────────────────────────────
        private static EficienciaDto MapToDto(Eficiencia e) => new()
        {
            Id             = e.Id,
            OrdemProducao  = e.OrdemProducao,
            Motivo         = e.Motivo,
            DataHoraInicio = e.DataHoraInicio,
            DataHoraFim    = e.DataHoraFim,
            Tempo          = e.Tempo ?? (e.DataHoraFim.HasValue ? e.DataHoraFim.Value - e.DataHoraInicio : null),
            EmAndamento    = !e.DataHoraFim.HasValue,
            Operador       = e.Operador,
            DataRegistro   = e.DataRegistro,
        };
    }
}
