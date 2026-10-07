using Microsoft.AspNetCore.Mvc;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResumoProducaoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ResumoProducaoController(AppDbContext context)
        {
            _context = context;
        }

        // DTOs para retorno estruturado
        public class ResumoItemDto
        {
            public string CodProduto { get; set; } = string.Empty;
            public string Produto { get; set; } = string.Empty;
            public string Unidade { get; set; } = string.Empty;
            public double MetaDia { get; set; }
            public double QtdProduzida { get; set; }
            public double Percentual { get; set; }
        }

        public class ResumoMensalItemDto
        {
            public string CodProduto { get; set; } = string.Empty;
            public string Produto { get; set; } = string.Empty;
            public string Unidade { get; set; } = string.Empty;
            public double Plano { get; set; }
            public double Realizado { get; set; }
            public double Percentual { get; set; }
        }

        public class ResumoGeralDto
        {
            public List<ResumoItemDto> Diario { get; set; } = new();
            public List<ResumoMensalItemDto> Mensal { get; set; } = new();
        }

        public class PaleteDetalheDto
        {
            public int Id { get; set; }
            public int N_Palete { get; set; }
            public string Lote { get; set; } = string.Empty;
            public double Quantidade { get; set; }
            public string HoraEntrada { get; set; } = string.Empty;
            public string Maquina { get; set; } = string.Empty;
            public string Usuario { get; set; } = string.Empty;
        }

        public class GrupoResumoDto
        {
            public string Chave { get; set; } = string.Empty;
            public double Quantidade { get; set; }
            public int NumPaletes { get; set; }
        }

        public class DetalheProducaoDto
        {
            public string CodProduto { get; set; } = string.Empty;
            public string Produto { get; set; } = string.Empty;
            public string Unidade { get; set; } = string.Empty;
            public List<PaleteDetalheDto> Paletes { get; set; } = new();
            public List<GrupoResumoDto> PorLote { get; set; } = new();
            public List<GrupoResumoDto> PorMaquina { get; set; } = new();
            public List<GrupoResumoDto> PorOperador { get; set; } = new();
        }

        [HttpGet("resumo")]
        public async Task<ActionResult<ResumoGeralDto>> GetResumo([FromQuery] string data)
        {
            if (!DateTime.TryParse(data, out var dataFiltro))
            {
                return BadRequest("Data inválida. Use o formato YYYY-MM-DD.");
            }

            var dia = dataFiltro.Day;
            var mes = dataFiltro.Month;
            var ano = dataFiltro.Year;

            var startOfMonth = new DateTime(ano, mes, 1);

            // 1. Obter Metas do Mês
            var metas = await _context.MetasProducao
                .Where(m => m.Mes == mes && m.Ano == ano)
                .ToListAsync();

            // 2. Obter Cadastros para nomes e unidades
            var cadastros = await _context.Cadastros.ToListAsync();

            // 3. Obter Paletizações do dia (filtra OPs canceladas ou deletadas)
            var paletizacoesDia = await _context.Paletizacoes
                .Include(p => p.Producao)
                .Where(p => p.Producao != null && p.Producao.DataHoraAbertura.Date == dataFiltro.Date && p.Producao.Status != "Cancelado")
                .ToListAsync();

            // 4. Obter Paletizações do mês até o dia selecionado (inclusive) (filtra OPs canceladas ou deletadas)
            var paletizacoesMes = await _context.Paletizacoes
                .Include(p => p.Producao)
                .Where(p => p.Producao != null && p.Producao.DataHoraAbertura.Date >= startOfMonth.Date && p.Producao.DataHoraAbertura.Date <= dataFiltro.Date && p.Producao.Status != "Cancelado")
                .ToListAsync();

            // Montar DTOs
            var diasNoMes = DateTime.DaysInMonth(ano, mes);

            // Mapeamento de produtos a considerar
            // Todos que têm meta, ou que tiveram produção no dia ou no mês
            var codigosProdutos = metas.Select(m => m.CodProduto)
                .Concat(cadastros.Select(c => c.CodProduto))
                .Concat(paletizacoesDia.Select(p => p.CodigoProduto))
                .Concat(paletizacoesMes.Select(p => p.CodigoProduto))
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            var resumoDiario = new List<ResumoItemDto>();
            var resumoMensal = new List<ResumoMensalItemDto>();

            foreach (var cod in codigosProdutos)
            {
                var cad = cadastros.FirstOrDefault(c => c.CodProduto == cod);
                var prodNome = cad?.Produto ?? paletizacoesDia.FirstOrDefault(p => p.CodigoProduto == cod)?.Produto ?? paletizacoesMes.FirstOrDefault(p => p.CodigoProduto == cod)?.Produto ?? cod;
                var unidade = cad?.Unidade ?? paletizacoesDia.FirstOrDefault(p => p.CodigoProduto == cod)?.Unidade ?? paletizacoesMes.FirstOrDefault(p => p.CodigoProduto == cod)?.Unidade ?? "UN";

                // Meta do mês para o produto
                var metaProd = metas.FirstOrDefault(m => m.CodProduto == cod);
                double metaMensalCaixas = metaProd?.MetaCaixas ?? 0;
                double metaDiaCaixas = metaMensalCaixas / diasNoMes;

                // Produção no dia
                double produzidaDia = paletizacoesDia.Where(p => p.CodigoProduto == cod).Sum(p => p.QtdePorPalete);

                // Produção no mês
                double produzidaMes = paletizacoesMes.Where(p => p.CodigoProduto == cod).Sum(p => p.QtdePorPalete);

                // Só incluir no resumo diário se teve produção no dia OR se tem meta diária > 0
                if (produzidaDia > 0 || metaDiaCaixas > 0)
                {
                    double percDiario = metaDiaCaixas > 0 ? (produzidaDia / metaDiaCaixas) * 100 : 0;
                    resumoDiario.Add(new ResumoItemDto
                    {
                        CodProduto = cod,
                        Produto = prodNome,
                        Unidade = unidade,
                        MetaDia = Math.Round(metaDiaCaixas, 2),
                        QtdProduzida = produzidaDia,
                        Percentual = Math.Round(percDiario, 2)
                    });
                }

                // Só incluir no resumo mensal se teve produção no mês OR se tem plano mensal > 0
                if (produzidaMes > 0 || metaMensalCaixas > 0)
                {
                    double percMensal = metaMensalCaixas > 0 ? (produzidaMes / metaMensalCaixas) * 100 : 0;
                    resumoMensal.Add(new ResumoMensalItemDto
                    {
                        CodProduto = cod,
                        Produto = prodNome,
                        Unidade = unidade,
                        Plano = Math.Round(metaMensalCaixas, 2),
                        Realizado = produzidaMes,
                        Percentual = Math.Round(percMensal, 2)
                    });
                }
            }

            var result = new ResumoGeralDto
            {
                Diario = resumoDiario.OrderBy(r => r.CodProduto).ToList(),
                Mensal = resumoMensal.OrderBy(r => r.CodProduto).ToList()
            };

            return Ok(result);
        }

        [HttpGet("detalhe")]
        public async Task<ActionResult<DetalheProducaoDto>> GetDetalhe([FromQuery] string codproduto, [FromQuery] string data)
        {
            if (string.IsNullOrEmpty(codproduto))
            {
                return BadRequest("O código do produto é obrigatório.");
            }

            if (!DateTime.TryParse(data, out var dataFiltro))
            {
                return BadRequest("Data inválida. Use o formato YYYY-MM-DD.");
            }

            // 1. Buscar cadastro
            var cadastro = await _context.Cadastros.FirstOrDefaultAsync(c => c.CodProduto == codproduto);

            // 2. Buscar paletizações para este produto e esta data (filtra OPs canceladas ou deletadas)
            var paletes = await _context.Paletizacoes
                .Include(p => p.Producao)
                .Where(p => p.CodigoProduto == codproduto && p.Producao != null && p.Producao.DataHoraAbertura.Date == dataFiltro.Date && p.Producao.Status != "Cancelado")
                .OrderBy(p => p.N_Palete)
                .ToListAsync();

            var produtoNome = cadastro?.Produto ?? paletes.FirstOrDefault()?.Produto ?? "Desconhecido";
            var unidade = cadastro?.Unidade ?? paletes.FirstOrDefault()?.Unidade ?? "UN";

            // DTO de paletes individuais
            var paletesDto = paletes.Select(p => new PaleteDetalheDto
            {
                Id = p.Id,
                N_Palete = p.N_Palete,
                Lote = string.IsNullOrWhiteSpace(p.Bloqueio) ? $"OP {p.OrdemProducao}" : p.Bloqueio,
                Quantidade = p.QtdePorPalete, // boxes/caixas
                HoraEntrada = p.DataHoraPaletizacao.ToString("HH:mm:ss"),
                Maquina = p.Maquina ?? "Sem Máquina",
                Usuario = p.Usuario ?? "Sem Usuário"
            }).ToList();

            // Agrupamentos
            // Por Lote (Lote é Bloqueio ou OP)
            var porLote = paletesDto
                .GroupBy(p => p.Lote)
                .Select(g => new GrupoResumoDto
                {
                    Chave = g.Key,
                    Quantidade = g.Sum(x => x.Quantidade),
                    NumPaletes = g.Count()
                })
                .OrderBy(x => x.Chave)
                .ToList();

            // Por Máquina
            var porMaquina = paletesDto
                .GroupBy(p => p.Maquina)
                .Select(g => new GrupoResumoDto
                {
                    Chave = g.Key,
                    Quantidade = g.Sum(x => x.Quantidade),
                    NumPaletes = g.Count()
                })
                .OrderBy(x => x.Chave)
                .ToList();

            // Por Operador (Usuario)
            var porOperador = paletesDto
                .GroupBy(p => p.Usuario)
                .Select(g => new GrupoResumoDto
                {
                    Chave = g.Key,
                    Quantidade = g.Sum(x => x.Quantidade),
                    NumPaletes = g.Count()
                })
                .OrderBy(x => x.Chave)
                .ToList();

            var result = new DetalheProducaoDto
            {
                CodProduto = codproduto,
                Produto = produtoNome,
                Unidade = unidade,
                Paletes = paletesDto,
                PorLote = porLote,
                PorMaquina = porMaquina,
                PorOperador = porOperador
            };

            return Ok(result);
        }
    }
}
