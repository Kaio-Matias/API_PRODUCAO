using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EstoqueAgranelController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EstoqueAgranelController(AppDbContext context)
        {
            _context = context;
        }

        // ── GET /api/EstoqueAgranel ────────────────────────────
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EstoqueAgranel>>> GetAll()
        {
            // Lista todos os A Granéis vinculados — garante registro zerado para quem nunca teve movimento
            var vinculos = await _context.VinculosAgranelAcabado.AsNoTracking().ToListAsync();
            var distinct = vinculos
                .GroupBy(v => v.CodigoAgranel)
                .Select(g => new { Codigo = g.Key, Descricao = g.First().DescricaoAgranel })
                .ToList();

            var estoques = await _context.EstoqueAgranel.ToListAsync();

            var resultado = distinct.Select(d =>
            {
                var est = estoques.FirstOrDefault(e => e.CodigoAgranel == d.Codigo);
                return est ?? new EstoqueAgranel
                {
                    CodigoAgranel = d.Codigo,
                    DescricaoAgranel = d.Descricao,
                    SaldoLitros = 0,
                    ValorUnitario = 0,
                    UltimaAtualizacao = DateTime.MinValue
                };
            }).OrderBy(e => e.CodigoAgranel).ToList();

            return Ok(resultado);
        }

        // ── GET /api/EstoqueAgranel/{codigo} ───────────────────
        [HttpGet("{codigo}")]
        public async Task<ActionResult<EstoqueAgranel>> Get(string codigo)
        {
            var estoque = await _context.EstoqueAgranel
                .FirstOrDefaultAsync(e => e.CodigoAgranel == codigo);
            if (estoque == null) return NotFound();
            return Ok(estoque);
        }

        // ── GET /api/EstoqueAgranel/{codigo}/movimentos ────────
        [HttpGet("{codigo}/movimentos")]
        public async Task<ActionResult<IEnumerable<MovimentoEstoqueAgranel>>> GetMovimentos(string codigo, [FromQuery] int? limit = 100)
        {
            var movs = await _context.MovimentosEstoqueAgranel
                .Where(m => m.CodigoAgranel == codigo)
                .OrderByDescending(m => m.DataMovimento)
                .Take(limit ?? 100)
                .ToListAsync();
            return Ok(movs);
        }

        // ── POST /api/EstoqueAgranel/ajustar ───────────────────
        public class AjusteEstoqueDto
        {
            public string CodigoAgranel { get; set; } = string.Empty;
            public double NovoSaldo { get; set; }
            public string? Observacao { get; set; }
            public string? Usuario { get; set; }
        }

        [HttpPost("ajustar")]
        public async Task<IActionResult> Ajustar([FromBody] AjusteEstoqueDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CodigoAgranel))
                return BadRequest("Código do A Granel é obrigatório.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var estoque = await _context.EstoqueAgranel
                    .FirstOrDefaultAsync(e => e.CodigoAgranel == dto.CodigoAgranel);

                double saldoAnterior = estoque?.SaldoLitros ?? 0;

                if (estoque == null)
                {
                    var vinc = await _context.VinculosAgranelAcabado
                        .FirstOrDefaultAsync(v => v.CodigoAgranel == dto.CodigoAgranel);

                    estoque = new EstoqueAgranel
                    {
                        CodigoAgranel = dto.CodigoAgranel,
                        DescricaoAgranel = vinc?.DescricaoAgranel ?? "A Granel",
                        SaldoLitros = dto.NovoSaldo,
                        ValorUnitario = 0,
                        UltimaAtualizacao = DateTime.UtcNow,
                        Observacoes = dto.Observacao
                    };
                    await _context.EstoqueAgranel.AddAsync(estoque);
                }
                else
                {
                    estoque.SaldoLitros = dto.NovoSaldo;
                    estoque.UltimaAtualizacao = DateTime.UtcNow;
                    estoque.Observacoes = dto.Observacao;
                    _context.EstoqueAgranel.Update(estoque);
                }

                await _context.MovimentosEstoqueAgranel.AddAsync(new MovimentoEstoqueAgranel
                {
                    CodigoAgranel = dto.CodigoAgranel,
                    TipoMovimento = "AJUSTE",
                    QuantidadeLitros = dto.NovoSaldo - saldoAnterior,
                    SaldoAnterior = saldoAnterior,
                    SaldoPosterior = dto.NovoSaldo,
                    DataMovimento = DateTime.UtcNow,
                    Referencia = "Ajuste manual",
                    Observacao = dto.Observacao,
                    Usuario = dto.Usuario
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(estoque);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }

        // ── POST /api/EstoqueAgranel/reset ──────────────────────
        // Zera o saldo de UM A Granel (ou de TODOS se codigoAgranel = null/"todos")
        // Mantém histórico de movimentos (append-only) — gera um movimento "RESET".
        public class ResetEstoqueDto
        {
            public string? CodigoAgranel { get; set; }  // null ou "todos" = zera tudo
            public bool LimparMovimentos { get; set; } = false;  // true = apaga audit trail também
            public bool LimparIndicadores { get; set; } = false; // true = apaga IndicadoresAgranel também
            public string? Usuario { get; set; }
            public string? Observacao { get; set; }
        }

        [HttpPost("reset")]
        public async Task<IActionResult> Reset([FromBody] ResetEstoqueDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool resetTodos = string.IsNullOrWhiteSpace(dto.CodigoAgranel)
                                   || dto.CodigoAgranel.Equals("todos", StringComparison.OrdinalIgnoreCase);

                var query = _context.EstoqueAgranel.AsQueryable();
                if (!resetTodos) query = query.Where(e => e.CodigoAgranel == dto.CodigoAgranel);
                var estoques = await query.ToListAsync();

                int countReset = 0;
                foreach (var est in estoques)
                {
                    double saldoAnterior = est.SaldoLitros;
                    if (saldoAnterior == 0) continue;

                    est.SaldoLitros = 0;
                    est.UltimaAtualizacao = DateTime.UtcNow;

                    await _context.MovimentosEstoqueAgranel.AddAsync(new MovimentoEstoqueAgranel
                    {
                        CodigoAgranel = est.CodigoAgranel,
                        TipoMovimento = "RESET",
                        QuantidadeLitros = -saldoAnterior,
                        SaldoAnterior = saldoAnterior,
                        SaldoPosterior = 0,
                        DataMovimento = DateTime.UtcNow,
                        Referencia = "Reset manual",
                        Observacao = dto.Observacao ?? "Saldo zerado pelo usuário.",
                        Usuario = dto.Usuario
                    });
                    countReset++;
                }

                // Se pediu para limpar o audit trail
                if (dto.LimparMovimentos)
                {
                    var movQuery = _context.MovimentosEstoqueAgranel.AsQueryable();
                    if (!resetTodos) movQuery = movQuery.Where(m => m.CodigoAgranel == dto.CodigoAgranel);
                    // Preserva apenas os RESETs recém-criados
                    var hoje = DateTime.UtcNow.AddSeconds(-30);
                    var movsAntigos = await movQuery.Where(m => m.DataMovimento < hoje).ToListAsync();
                    _context.MovimentosEstoqueAgranel.RemoveRange(movsAntigos);
                }

                // Se pediu para limpar os indicadores diários
                if (dto.LimparIndicadores)
                {
                    var indQuery = _context.IndicadoresAgranel.AsQueryable();
                    if (!resetTodos) indQuery = indQuery.Where(i => i.CodigoAgranel == dto.CodigoAgranel);
                    var inds = await indQuery.ToListAsync();
                    _context.IndicadoresAgranel.RemoveRange(inds);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = resetTodos
                        ? $"Reset aplicado em {countReset} A Granel(is)."
                        : $"A Granel {dto.CodigoAgranel} zerado.",
                    estoquesZerados = countReset,
                    movimentosApagados = dto.LimparMovimentos,
                    indicadoresApagados = dto.LimparIndicadores
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }

        // ── POST /api/EstoqueAgranel/reset-consumo-dia ──────────
        // Zera o CONSUMO do indicador de um dia específico (apaga o registro diário).
        // Como o consumo é recalculado dinamicamente, a tela voltará a mostrar 0
        // APENAS se não houver paletes daquele dia. Se houver paletes, o consumo
        // volta a aparecer — para realmente zerar, também apague/desconsidere os paletes.
        public class ResetConsumoDiaDto
        {
            public DateTime Data { get; set; }
            public string? CodigoAgranel { get; set; }  // null = todos
        }

        [HttpPost("reset-consumo-dia")]
        public async Task<IActionResult> ResetConsumoDia([FromBody] ResetConsumoDiaDto dto)
        {
            var data = dto.Data.Date;

            // UPSERT: cria/atualiza registro com Consumo=0 e marker [RESET] para travar o cálculo dinâmico
            var vinculos = await _context.VinculosAgranelAcabado.AsNoTracking().ToListAsync();
            var agranelAlvo = string.IsNullOrWhiteSpace(dto.CodigoAgranel)
                ? vinculos.GroupBy(v => v.CodigoAgranel).Select(g => new { Codigo = g.Key, Descricao = g.First().DescricaoAgranel }).ToList()
                : vinculos.Where(v => v.CodigoAgranel == dto.CodigoAgranel)
                          .GroupBy(v => v.CodigoAgranel).Select(g => new { Codigo = g.Key, Descricao = g.First().DescricaoAgranel }).ToList();

            if (!agranelAlvo.Any())
                return BadRequest("Nenhum A Granel encontrado para o filtro informado.");

            int alterados = 0;
            foreach (var alvo in agranelAlvo)
            {
                var existente = await _context.IndicadoresAgranel
                    .FirstOrDefaultAsync(i => i.DataReferencia.Date == data && i.CodigoAgranel == alvo.Codigo);

                if (existente != null)
                {
                    existente.Consumo = 0;
                    existente.Perdas = existente.Inicial + existente.Preparado - 0 - existente.Final;
                    existente.PerdaValorizada = (decimal)existente.Perdas * existente.ValorUnitario;
                    existente.PercentualPerda = 0;
                    existente.Observacoes = $"[RESET] Consumo travado em 0 por reset manual em {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
                    _context.IndicadoresAgranel.Update(existente);
                }
                else
                {
                    await _context.IndicadoresAgranel.AddAsync(new IndicadorAgranel
                    {
                        DataReferencia = data,
                        CodigoAgranel = alvo.Codigo,
                        DescricaoAgranel = alvo.Descricao,
                        Inicial = 0,
                        Preparado = 0,
                        Consumo = 0,
                        Final = 0,
                        Perdas = 0,
                        PercentualPerda = 0,
                        PerdaValorizada = 0,
                        ValorUnitario = 0,
                        Observacoes = $"[RESET] Criado por reset manual em {DateTime.UtcNow:yyyy-MM-dd HH:mm}"
                    });
                }
                alterados++;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Consumo travado em 0 para {alterados} A Granel(is) no dia {data:yyyy-MM-dd}. Para reativar o cálculo dinâmico, limpe o campo 'Observacoes' ou apague o registro.",
                alterados
            });
        }

        // ── POST /api/EstoqueAgranel/purgar-paletes-dia ─────────
        // PERIGOSO: Apaga TODOS os paletes registrados no dia informado.
        // Use apenas para limpar dados de TESTE.
        public class PurgarPaletesDiaDto
        {
            public DateTime Data { get; set; }
            public string? CodigoAgranel { get; set; }  // null = todos
            public string? Confirmacao { get; set; }     // deve ser exatamente "APAGAR"
        }

        [HttpPost("purgar-paletes-dia")]
        public async Task<IActionResult> PurgarPaletesDia([FromBody] PurgarPaletesDiaDto dto)
        {
            if (dto.Confirmacao != "APAGAR")
                return BadRequest("Para confirmar a operação destrutiva, envie o campo 'confirmacao' com o valor exato \"APAGAR\".");

            var data = dto.Data.Date;

            var query = _context.Paletizacoes
                .Include(p => p.Producao)
                .Where(p => p.DataHoraPaletizacao.Date == data);

            // Filtro opcional: apenas paletes cuja OP esteja vinculada ao A Granel especificado
            if (!string.IsNullOrWhiteSpace(dto.CodigoAgranel))
            {
                var vinculos = await _context.VinculosAgranelAcabado
                    .Where(v => v.CodigoAgranel == dto.CodigoAgranel)
                    .ToListAsync();
                var codigosAcabados = vinculos.Select(v => v.CodigoProdutoAcabado).ToHashSet();

                query = query.Where(p =>
                    (p.Producao != null && p.Producao.CodigoAgranel == dto.CodigoAgranel)
                    || (p.CodigoProduto != null && codigosAcabados.Contains(p.CodigoProduto)));
            }

            var paletes = await query.ToListAsync();
            if (!paletes.Any())
                return Ok(new { message = "Nenhum palete encontrado no filtro informado.", removidos = 0 });

            _context.Paletizacoes.RemoveRange(paletes);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"{paletes.Count} palete(s) apagado(s) do dia {data:yyyy-MM-dd}.",
                removidos = paletes.Count
            });
        }

        // ── POST /api/EstoqueAgranel/entrada ────────────────────
        public class EntradaEstoqueDto
        {
            public string CodigoAgranel { get; set; } = string.Empty;
            public double Quantidade { get; set; }
            public string? Observacao { get; set; }
            public string? Usuario { get; set; }
        }

        [HttpPost("entrada")]
        public async Task<IActionResult> Entrada([FromBody] EntradaEstoqueDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CodigoAgranel) || dto.Quantidade <= 0)
                return BadRequest("Código e quantidade (> 0) são obrigatórios.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var estoque = await _context.EstoqueAgranel
                    .FirstOrDefaultAsync(e => e.CodigoAgranel == dto.CodigoAgranel);

                double saldoAnterior = estoque?.SaldoLitros ?? 0;
                double saldoNovo = saldoAnterior + dto.Quantidade;

                if (estoque == null)
                {
                    var vinc = await _context.VinculosAgranelAcabado
                        .FirstOrDefaultAsync(v => v.CodigoAgranel == dto.CodigoAgranel);

                    estoque = new EstoqueAgranel
                    {
                        CodigoAgranel = dto.CodigoAgranel,
                        DescricaoAgranel = vinc?.DescricaoAgranel ?? "A Granel",
                        SaldoLitros = saldoNovo,
                        ValorUnitario = 0,
                        UltimaAtualizacao = DateTime.UtcNow
                    };
                    await _context.EstoqueAgranel.AddAsync(estoque);
                }
                else
                {
                    estoque.SaldoLitros = saldoNovo;
                    estoque.UltimaAtualizacao = DateTime.UtcNow;
                    _context.EstoqueAgranel.Update(estoque);
                }

                await _context.MovimentosEstoqueAgranel.AddAsync(new MovimentoEstoqueAgranel
                {
                    CodigoAgranel = dto.CodigoAgranel,
                    TipoMovimento = "ENTRADA_INICIAL",
                    QuantidadeLitros = dto.Quantidade,
                    SaldoAnterior = saldoAnterior,
                    SaldoPosterior = saldoNovo,
                    DataMovimento = DateTime.UtcNow,
                    Referencia = "Entrada manual",
                    Observacao = dto.Observacao,
                    Usuario = dto.Usuario
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(estoque);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }

        // ── POST /api/EstoqueAgranel/atualizar-custos ───────────
        // Atualiza o ValorUnitario (custo por litro) de múltiplos A Granéis de uma vez.
        public class AtualizarCustosDto
        {
            public List<CustoItem> Custos { get; set; } = new();
            public class CustoItem
            {
                public string CodigoAgranel { get; set; } = string.Empty;
                public decimal ValorUnitario { get; set; }
            }
        }

        [HttpPost("atualizar-custos")]
        public async Task<IActionResult> AtualizarCustos([FromBody] AtualizarCustosDto dto)
        {
            if (dto?.Custos == null || !dto.Custos.Any())
                return BadRequest("Nenhum custo informado.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int atualizados = 0;
                foreach (var item in dto.Custos)
                {
                    if (string.IsNullOrWhiteSpace(item.CodigoAgranel)) continue;

                    var estoque = await _context.EstoqueAgranel
                        .FirstOrDefaultAsync(e => e.CodigoAgranel == item.CodigoAgranel);

                    if (estoque != null)
                    {
                        estoque.ValorUnitario = item.ValorUnitario;
                        estoque.UltimaAtualizacao = DateTime.UtcNow;
                        _context.EstoqueAgranel.Update(estoque);
                    }
                    else
                    {
                        var vinc = await _context.VinculosAgranelAcabado
                            .FirstOrDefaultAsync(v => v.CodigoAgranel == item.CodigoAgranel);
                        estoque = new EstoqueAgranel
                        {
                            CodigoAgranel = item.CodigoAgranel,
                            DescricaoAgranel = vinc?.DescricaoAgranel ?? item.CodigoAgranel,
                            SaldoLitros = 0,
                            ValorUnitario = item.ValorUnitario,
                            UltimaAtualizacao = DateTime.UtcNow
                        };
                        await _context.EstoqueAgranel.AddAsync(estoque);
                    }
                    atualizados++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = $"Custo atualizado para {atualizados} A Granel(is).", atualizados });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }
    }
}
