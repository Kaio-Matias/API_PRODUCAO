using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IndicadoresAgranelController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IndicadoresAgranelController(AppDbContext context)
        {
            _context = context;
        }

        private Task<Dictionary<string, double>> CalcularConsumoPorAgranelAsync(DateTime dataRef)
            => CalcularConsumoRangeAsync(dataRef, dataRef);

        private async Task<Dictionary<string, double>> CalcularConsumoRangeAsync(DateTime inicio, DateTime fim)
        {
            var consumo = new Dictionary<string, double>();

            // Filtra por DataHoraFECHAMENTO (não abertura): o consumo é consumado quando a
            // OP encerra, independentemente de quando ela foi aberta.
            // Ex.: OP aberta na seg e fechada na ter aparece no consumo da ter.
            var ops = await _context.Producoes
                .AsNoTracking()
                .Include(p => p.DetalhamentoOPs)
                .Where(p => p.Status == "Fechado" &&
                            p.DataHoraFechamento.HasValue &&
                            p.DataHoraFechamento!.Value.Date >= inicio.Date &&
                            p.DataHoraFechamento!.Value.Date <= fim.Date &&
                            p.CodigoAgranel != null && p.CodigoAgranel != "" &&
                            p.FatorConversaoLiters > 0)
                .ToListAsync();

            foreach (var op in ops)
            {
                // Soma EmbProduzidas de todos os turnos da OP
                int totalEmb = op.DetalhamentoOPs?.Sum(d => d.EmbProduzidas) ?? 0;
                if (totalEmb <= 0) continue;

                // Consumo (L) = embalagens produzidas × fator de conversão (litros/embalagem)
                // Ex: 15.000 emb de leite 1L × 1.0 = 15.000 L
                double litros = totalEmb * op.FatorConversaoLiters;

                if (!consumo.ContainsKey(op.CodigoAgranel!))
                    consumo[op.CodigoAgranel!] = 0;
                consumo[op.CodigoAgranel!] += litros;
            }

            return consumo;
        }

        [HttpGet("{data}")]
        public async Task<ActionResult<IEnumerable<IndicadorAgranel>>> GetDia(DateTime data)
        {
            var dataReferencia = data.Date;
            var estoques = await _context.EstoqueAgranel.AsNoTracking().ToListAsync();
            var vinculos = await _context.VinculosAgranelAcabado.ToListAsync();
            var agranelCodigosDistintos = vinculos
                .Select(v => new { v.CodigoAgranel, v.DescricaoAgranel })
                .DistinctBy(v => v.CodigoAgranel)
                .ToList();

            var consumoPorAgranel = await CalcularConsumoPorAgranelAsync(dataReferencia);
            var registrosDia = await _context.IndicadoresAgranel
                .Where(x => x.DataReferencia.Date == dataReferencia)
                .ToListAsync();

            var ultimoDiaRegistrado = await _context.IndicadoresAgranel
                .Where(x => x.DataReferencia.Date < dataReferencia)
                .OrderByDescending(x => x.DataReferencia)
                .Select(x => x.DataReferencia)
                .FirstOrDefaultAsync();

            var registrosAnteriores = new List<IndicadorAgranel>();
            if (ultimoDiaRegistrado != default)
            {
                registrosAnteriores = await _context.IndicadoresAgranel
                    .Where(x => x.DataReferencia.Date == ultimoDiaRegistrado.Date)
                    .ToListAsync();
            }

            var resultados = new List<IndicadorAgranel>();

            foreach (var item in agranelCodigosDistintos)
            {
                double consumoCalculado = consumoPorAgranel.TryGetValue(item.CodigoAgranel, out var c) ? c : 0;
                var salvo = registrosDia.FirstOrDefault(r => r.CodigoAgranel == item.CodigoAgranel);
                if (salvo != null)
                {
                    bool isResetOverride = !string.IsNullOrEmpty(salvo.Observacoes)
                                            && salvo.Observacoes.Contains("[RESET]", StringComparison.OrdinalIgnoreCase);
                    if (!isResetOverride)
                        salvo.Consumo = consumoCalculado;
                    RecalcularIndicadores(salvo);
                    resultados.Add(salvo);
                }
                else
                {
                    var ant = registrosAnteriores.FirstOrDefault(r => r.CodigoAgranel == item.CodigoAgranel);
                    double inicialAtual = CalcularInicial(ant);
                    var estUnit = estoques.FirstOrDefault(e => e.CodigoAgranel == item.CodigoAgranel);
                    decimal valorUnit = 0;
                    if (ant != null && ant.ValorUnitario > 0) valorUnit = ant.ValorUnitario;
                    else if (estUnit != null && estUnit.ValorUnitario > 0) valorUnit = estUnit.ValorUnitario;

                    var novo = new IndicadorAgranel
                    {
                        DataReferencia = dataReferencia,
                        CodigoAgranel = item.CodigoAgranel,
                        DescricaoAgranel = item.DescricaoAgranel,
                        Inicial = inicialAtual,
                        Preparado = 0,
                        Consumo = consumoCalculado,
                        Final = 0,
                        ValorUnitario = valorUnit,
                    };
                    RecalcularIndicadores(novo);
                    resultados.Add(novo);
                }
            }

            return Ok(resultados.OrderBy(r => r.CodigoAgranel));
        }

        private static void RecalcularIndicadores(IndicadorAgranel ind)
        {
            // Perdas = Consumo + Final − Inicial − Preparado
            // Resultado negativo = houve perda real (material não contabilizado)
            ind.Perdas = ind.Consumo + ind.Final - ind.Inicial - ind.Preparado;
            ind.PerdaValorizada = (decimal)ind.Perdas * ind.ValorUnitario;
            double denom = ind.Inicial + ind.Preparado;
            ind.PercentualPerda = denom > 0 ? (ind.Perdas / denom) * 100 : 0;
        }

        /// <summary>
        /// Calcula o Inicial do próximo dia com base no registro anterior.
        /// Se o Final foi fisicamente contado (> 0), usa esse valor.
        /// Caso contrário, propaga o saldo teórico: Inicial + Preparado − Consumo.
        /// Isso garante que preparado lançado em 31/03 apareça como Inicial em 01/04
        /// mesmo que o estoque final do dia não tenha sido fechado.
        /// </summary>
        private static double CalcularInicial(IndicadorAgranel? ant)
        {
            if (ant == null) return 0;
            if (ant.Final > 0) return ant.Final;
            return Math.Max(0, ant.Inicial + ant.Preparado - ant.Consumo);
        }

        [HttpGet("periodo")]
        public async Task<IActionResult> GetPeriodo([FromQuery] DateTime inicio, [FromQuery] DateTime fim)
        {
            var dataInicio = inicio.Date;
            var dataFim = fim.Date;

            var vinculos = await _context.VinculosAgranelAcabado.AsNoTracking().ToListAsync();
            var agranelCodigosDistintos = vinculos
                .Select(v => new { v.CodigoAgranel, v.DescricaoAgranel })
                .DistinctBy(v => v.CodigoAgranel)
                .ToList();

            var consumoPorAgranel = await CalcularConsumoRangeAsync(dataInicio, dataFim);
            var registrosPeriodo = await _context.IndicadoresAgranel
                .Where(x => x.DataReferencia.Date >= dataInicio && x.DataReferencia.Date <= dataFim)
                .ToListAsync();

            var ultimoDiaAnterior = await _context.IndicadoresAgranel
                .Where(x => x.DataReferencia.Date < dataInicio)
                .OrderByDescending(x => x.DataReferencia)
                .Select(x => x.DataReferencia)
                .FirstOrDefaultAsync();

            var registrosAnteriores = new List<IndicadorAgranel>();
            if (ultimoDiaAnterior != default)
            {
                registrosAnteriores = await _context.IndicadoresAgranel
                    .Where(x => x.DataReferencia.Date == ultimoDiaAnterior.Date)
                    .ToListAsync();
            }

            var estoques = await _context.EstoqueAgranel.AsNoTracking().ToListAsync();

            var resultados = agranelCodigosDistintos.Select(item =>
            {
                double consumo = consumoPorAgranel.TryGetValue(item.CodigoAgranel, out var c) ? c : 0;
                double preparado = registrosPeriodo.Where(r => r.CodigoAgranel == item.CodigoAgranel).Sum(r => r.Preparado);

                var primeiroDia = registrosPeriodo
                    .Where(r => r.CodigoAgranel == item.CodigoAgranel && r.DataReferencia.Date == dataInicio)
                    .FirstOrDefault();

                double inicial = 0;
                if (primeiroDia != null && primeiroDia.Inicial > 0)
                    inicial = primeiroDia.Inicial;
                else
                {
                    var ant = registrosAnteriores.FirstOrDefault(r => r.CodigoAgranel == item.CodigoAgranel);
                    inicial = CalcularInicial(ant);
                }

                var maisRecente = registrosPeriodo
                    .Where(r => r.CodigoAgranel == item.CodigoAgranel)
                    .OrderByDescending(r => r.DataReferencia)
                    .FirstOrDefault();
                double final_ = maisRecente?.Final ?? 0;

                var ant_ = registrosAnteriores.FirstOrDefault(r => r.CodigoAgranel == item.CodigoAgranel);
                var estUnit = estoques.FirstOrDefault(e => e.CodigoAgranel == item.CodigoAgranel);
                decimal valorUnitario = 0;
                if (maisRecente != null && maisRecente.ValorUnitario > 0) valorUnitario = maisRecente.ValorUnitario;
                else if (ant_ != null && ant_.ValorUnitario > 0) valorUnitario = ant_.ValorUnitario;
                else if (estUnit != null && estUnit.ValorUnitario > 0) valorUnitario = estUnit.ValorUnitario;

                double perdas = consumo + final_ - inicial - preparado;
                decimal perdaValorizada = (decimal)perdas * valorUnitario;
                double denom = inicial + preparado;
                double percentualPerda = denom > 0 ? (perdas / denom) * 100 : 0;

                return new
                {
                    codigoAgranel = item.CodigoAgranel,
                    descricaoAgranel = item.DescricaoAgranel,
                    inicial,
                    preparado,
                    consumo,
                    final = final_,
                    perdas,
                    valorUnitario,
                    perdaValorizada,
                    percentualPerda
                };
            })
            .OrderBy(r => r.codigoAgranel)
            .ToList();

            return Ok(resultados);
        }

        [HttpPost("salvarLote")]
        public async Task<IActionResult> PostLote([FromBody] List<IndicadorAgranel> indicadores)
        {
            if (indicadores == null || !indicadores.Any()) return BadRequest();
            var dataRef = indicadores.First().DataReferencia.Date;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existentes = await _context.IndicadoresAgranel.Where(i => i.DataReferencia.Date == dataRef).ToListAsync();
                _context.IndicadoresAgranel.RemoveRange(existentes);

                var consumoPorAgranel = await CalcularConsumoPorAgranelAsync(dataRef);
                foreach (var ind in indicadores)
                {
                    ind.Consumo = consumoPorAgranel.TryGetValue(ind.CodigoAgranel, out var c) ? c : 0;
                    RecalcularIndicadores(ind);
                }

                await _context.IndicadoresAgranel.AddRangeAsync(indicadores);

                foreach (var ind in indicadores)
                {
                    var estoque = await _context.EstoqueAgranel.FirstOrDefaultAsync(e => e.CodigoAgranel == ind.CodigoAgranel);
                    double saldoAnterior = estoque?.SaldoLitros ?? 0;

                    if (estoque == null)
                    {
                        estoque = new EstoqueAgranel {
                            CodigoAgranel = ind.CodigoAgranel,
                            DescricaoAgranel = ind.DescricaoAgranel,
                            SaldoLitros = ind.Final,
                            ValorUnitario = ind.ValorUnitario,
                            UltimaAtualizacao = DateTime.UtcNow
                        };
                        await _context.EstoqueAgranel.AddAsync(estoque);
                    }
                    else
                    {
                        estoque.SaldoLitros = ind.Final;
                        estoque.ValorUnitario = ind.ValorUnitario;
                        estoque.UltimaAtualizacao = DateTime.UtcNow;
                        _context.EstoqueAgranel.Update(estoque);
                    }

                    await _context.MovimentosEstoqueAgranel.AddAsync(new MovimentoEstoqueAgranel {
                        CodigoAgranel = ind.CodigoAgranel,
                        TipoMovimento = "FECHAMENTO_DIA",
                        QuantidadeLitros = ind.Final - saldoAnterior,
                        SaldoAnterior = saldoAnterior,
                        SaldoPosterior = ind.Final,
                        DataMovimento = DateTime.UtcNow,
                        Referencia = $"Indicador {dataRef:yyyy-MM-dd}"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "OK" });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("adicionar-preparo")]
        public async Task<IActionResult> AdicionarPreparo([FromBody] Valedourado.Shared.Dtos.AdicionarPreparoDto dto)
        {
            var dataRef = dto.DataReferencia ?? DateTime.UtcNow.Date;
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var registroDia = await _context.IndicadoresAgranel.FirstOrDefaultAsync(i => i.DataReferencia.Date == dataRef && i.CodigoAgranel == dto.CodigoAgranel);
                var consumoMap = await CalcularConsumoPorAgranelAsync(dataRef);
                double consumoAtual = consumoMap.TryGetValue(dto.CodigoAgranel, out var c) ? c : 0;

                if (registroDia != null)
                {
                    registroDia.Preparado += dto.Quantidade;
                    registroDia.Consumo = consumoAtual;
                    RecalcularIndicadores(registroDia);
                    _context.IndicadoresAgranel.Update(registroDia);
                }
                else
                {
                    var vinculo = await _context.VinculosAgranelAcabado.FirstOrDefaultAsync(v => v.CodigoAgranel == dto.CodigoAgranel);
                    var ultimo = await _context.IndicadoresAgranel.Where(x => x.DataReferencia.Date < dataRef && x.CodigoAgranel == dto.CodigoAgranel).OrderByDescending(x => x.DataReferencia).ToListAsync();
                    var ant = ultimo.FirstOrDefault();

                    registroDia = new IndicadorAgranel {
                        DataReferencia = dataRef,
                        CodigoAgranel = dto.CodigoAgranel,
                        DescricaoAgranel = vinculo?.DescricaoAgranel ?? dto.CodigoAgranel,
                        Inicial = CalcularInicial(ant),
                        Preparado = dto.Quantidade,
                        Consumo = consumoAtual,
                        Final = 0,
                        ValorUnitario = ant?.ValorUnitario ?? 0
                    };
                    RecalcularIndicadores(registroDia);
                    await _context.IndicadoresAgranel.AddAsync(registroDia);
                }

                var estoque = await _context.EstoqueAgranel.FirstOrDefaultAsync(e => e.CodigoAgranel == dto.CodigoAgranel);
                double saldoAnterior = estoque?.SaldoLitros ?? 0;
                double saldoNovo = saldoAnterior + dto.Quantidade;

                if (estoque == null)
                {
                    await _context.EstoqueAgranel.AddAsync(new EstoqueAgranel { 
                        CodigoAgranel = dto.CodigoAgranel, 
                        DescricaoAgranel = registroDia.DescricaoAgranel,
                        SaldoLitros = saldoNovo,
                        ValorUnitario = registroDia.ValorUnitario,
                        UltimaAtualizacao = DateTime.UtcNow
                    });
                }
                else
                {
                    estoque.SaldoLitros = saldoNovo;
                    _context.EstoqueAgranel.Update(estoque);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "Lançado." });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet("preparado-mensal")]
        public async Task<ActionResult> GetPreparadoMensal([FromQuery] int ano, [FromQuery] int mes)
        {
            var inicio = new DateTime(ano, mes, 1);
            var fim = inicio.AddMonths(1).AddDays(-1);
            int diasNoMes = DateTime.DaysInMonth(ano, mes);

            var vinculos = await _context.VinculosAgranelAcabado.AsNoTracking().ToListAsync();
            var agraneis = vinculos.Select(v => new { v.CodigoAgranel, v.DescricaoAgranel }).DistinctBy(v => v.CodigoAgranel).OrderBy(v => v.CodigoAgranel).ToList();
            var registros = await _context.IndicadoresAgranel.Where(i => i.DataReferencia.Date >= inicio && i.DataReferencia.Date <= fim).AsNoTracking().ToListAsync();

            var ultimoMesAnterior = await _context.IndicadoresAgranel.Where(i => i.DataReferencia.Date < inicio).OrderByDescending(i => i.DataReferencia).Select(i => i.DataReferencia).FirstOrDefaultAsync();
            var registrosAnteriores = new List<IndicadorAgranel>();
            if (ultimoMesAnterior != default)
            {
                registrosAnteriores = await _context.IndicadoresAgranel.Where(i => i.DataReferencia.Date == ultimoMesAnterior.Date).AsNoTracking().ToListAsync();
            }

            var itens = agraneis.Select(a =>
            {
                var ant = registrosAnteriores.FirstOrDefault(r => r.CodigoAgranel == a.CodigoAgranel);
                var porDia = new Dictionary<int, double>();
                for (int d = 1; d <= diasNoMes; d++)
                {
                    var reg = registros.FirstOrDefault(r => r.CodigoAgranel == a.CodigoAgranel && r.DataReferencia.Day == d);
                    if (reg != null && reg.Preparado > 0) porDia[d] = reg.Preparado;
                }

                return new {
                    codigoAgranel = a.CodigoAgranel,
                    descricaoAgranel = a.DescricaoAgranel,
                    inicial = CalcularInicial(ant),
                    preparadoPorDia = porDia,
                    totalPreparado = porDia.Values.Sum()
                };
            }).ToList();

            return Ok(new { ano, mes, diasNoMes, itens });
        }

        public class LancarPreparadoLoteDto
        {
            public int Ano { get; set; }
            public int Mes { get; set; }
            public List<PreparadoItem> Itens { get; set; } = new();
            public class PreparadoItem {
                public string CodigoAgranel { get; set; } = string.Empty;
                public int Dia { get; set; }
                public double Quantidade { get; set; }
            }
        }

        [HttpPost("lancar-preparado-lote")]
        public async Task<IActionResult> LancarPreparadoLote([FromBody] LancarPreparadoLoteDto dto)
        {
            var vinculos = await _context.VinculosAgranelAcabado.AsNoTracking().ToListAsync();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try {
                foreach (var item in dto.Itens) {
                    var dataRef = new DateTime(dto.Ano, dto.Mes, item.Dia).Date;
                    var registro = await _context.IndicadoresAgranel.FirstOrDefaultAsync(i => i.DataReferencia.Date == dataRef && i.CodigoAgranel == item.CodigoAgranel);
                    if (registro != null) {
                        registro.Preparado = item.Quantidade;
                        RecalcularIndicadores(registro);
                    } else {
                        var ant = await _context.IndicadoresAgranel.Where(i => i.DataReferencia.Date < dataRef && i.CodigoAgranel == item.CodigoAgranel).OrderByDescending(i => i.DataReferencia).FirstOrDefaultAsync();
                        var vinc = vinculos.FirstOrDefault(v => v.CodigoAgranel == item.CodigoAgranel);
                        var novo = new IndicadorAgranel {
                            DataReferencia = dataRef,
                            CodigoAgranel = item.CodigoAgranel,
                            DescricaoAgranel = vinc?.DescricaoAgranel ?? item.CodigoAgranel,
                            Inicial = CalcularInicial(ant),
                            Preparado = item.Quantidade,
                            ValorUnitario = ant?.ValorUnitario ?? 0
                        };
                        RecalcularIndicadores(novo);
                        await _context.IndicadoresAgranel.AddAsync(novo);
                    }
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "OK" });
            } catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        public class SalvarFinalPeriodoDto
        {
            public DateTime Data { get; set; }
            public List<FinalItem> Itens { get; set; } = new();
            public class FinalItem {
                public string CodigoAgranel { get; set; } = string.Empty;
                public double Inicial { get; set; }
                public double Preparado { get; set; }
                public double Final { get; set; }
            }
        }

        [HttpPost("sincronizar-fatores")]
        public async Task<IActionResult> SincronizarFatores()
        {
            // Atualiza CodigoAgranel e FatorConversaoLiters em todas as OPs que ainda não têm esses campos
            // preenchidos, buscando o vínculo pelo código do produto acabado
            var vinculos = await _context.VinculosAgranelAcabado.AsNoTracking().ToListAsync();
            var ops = await _context.Producoes
                .Where(p => p.CodigoAgranel == null || p.CodigoAgranel == "" || p.FatorConversaoLiters == 0)
                .ToListAsync();

            int atualizadas = 0;
            foreach (var op in ops)
            {
                if (string.IsNullOrEmpty(op.Produto)) continue;
                var idx = op.Produto.IndexOf('-');
                var codigoProduto = idx > 0 ? op.Produto[..idx].Trim() : op.Produto.Trim();
                var vinc = vinculos.FirstOrDefault(v => v.CodigoProdutoAcabado == codigoProduto);
                if (vinc == null) continue;
                op.CodigoAgranel = vinc.CodigoAgranel;
                op.FatorConversaoLiters = vinc.LitrosPorCaixa;
                atualizadas++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { atualizadas, semVinculo = ops.Count - atualizadas });
        }

        [HttpPost("salvar-final-periodo")]
        public async Task<IActionResult> SalvarFinalPeriodo([FromBody] SalvarFinalPeriodoDto dto)
        {
            var dataRef = dto.Data.Date;
            var consumoMap = await CalcularConsumoPorAgranelAsync(dataRef);
            foreach (var item in dto.Itens) {
                var registro = await _context.IndicadoresAgranel.FirstOrDefaultAsync(i => i.DataReferencia.Date == dataRef && i.CodigoAgranel == item.CodigoAgranel);
                if (registro != null) {
                    registro.Inicial = item.Inicial;
                    registro.Preparado = item.Preparado;
                    registro.Final = item.Final;
                    registro.Consumo = consumoMap.TryGetValue(item.CodigoAgranel, out var c) ? c : registro.Consumo;
                    RecalcularIndicadores(registro);
                } else {
                    var vinculo = await _context.VinculosAgranelAcabado.FirstOrDefaultAsync(v => v.CodigoAgranel == item.CodigoAgranel);
                    var novo = new IndicadorAgranel {
                        DataReferencia = dataRef,
                        CodigoAgranel = item.CodigoAgranel,
                        DescricaoAgranel = vinculo?.DescricaoAgranel ?? item.CodigoAgranel,
                        Inicial = item.Inicial,
                        Preparado = item.Preparado,
                        Final = item.Final,
                        Consumo = consumoMap.TryGetValue(item.CodigoAgranel, out var c) ? c : 0
                    };
                    RecalcularIndicadores(novo);
                    await _context.IndicadoresAgranel.AddAsync(novo);
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "OK" });
        }
    }
}
