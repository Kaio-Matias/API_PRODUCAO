using API_PRODUCAO.Data;
using API_PRODUCAO.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Borders;
using Valedourado.Shared.Dtos;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using iText.Kernel.Colors;

// Usando aliases para resolver ambiguidades com a biblioteca iText
using Document = iText.Layout.Document;
using Image = iText.Layout.Element.Image;
using Paragraph = iText.Layout.Element.Paragraph;
using Table = iText.Layout.Element.Table;
using Cell = iText.Layout.Element.Cell;
using TextAlignment = iText.Layout.Properties.TextAlignment;
using UnitValue = iText.Layout.Properties.UnitValue;
using Text = iText.Layout.Element.Text;

namespace API_PRODUCAO.Services
{
    public class RelatorioService : IRelatorioService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public RelatorioService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<RelatorioOpCompletoDto?> GetRelatorioCompletoOpAsync(int ordemProducao)
        {
            var producao = await _context.Producoes
                .AsNoTracking()
                .Include(p => p.DetalhamentoOPs)
                .Include(p => p.Perdas)
                .Include(p => p.Eficiencia)
                .Include(p => p.Paletizacoes)
                .FirstOrDefaultAsync(p => p.OrdemProducao == ordemProducao);

            if (producao == null) return null;

            var relatorioDto = _mapper.Map<RelatorioOpCompletoDto>(producao);
            return relatorioDto;
        }

        public async Task<DashboardDto> GetDashboardDataAsync()
        {
            var hoje = DateTime.Today;
            var opsAbertas = await _context.Producoes.CountAsync(p => p.Status == "Aberto");
            var totalProduzido = await _context.DetalhamentoOPs.Where(d => d.Producao.DataHoraAbertura.Date == hoje).SumAsync(d => d.EmbProduzidas);
            var totalPerdido = await _context.Perdas.Where(p => p.Producao.DataHoraAbertura.Date == hoje).SumAsync(p => p.Quantidade);

            return new DashboardDto
            {
                OpsAbertas = opsAbertas,
                TotalProduzidoHoje = totalProduzido,
                TotalPerdidoHoje = totalPerdido
            };
        }

        public async Task<byte[]> GerarPdfDeRelatorioAsync(int ordemProducao)
        {
            var relatorio = await GetRelatorioCompletoOpAsync(ordemProducao);
            if (relatorio == null)
            {
                throw new FileNotFoundException($"Dados da Ordem de Produção {ordemProducao} não encontrados.");
            }

            using var memoryStream = new MemoryStream();
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

            document.SetFont(font).SetFontSize(10);

            // ================== CABEÇALHO ==================
            string imagePath = Path.Combine("wwwroot", "img", "logo.png");
            if (File.Exists(imagePath))
            {
                Image logo = new Image(ImageDataFactory.Create(imagePath))
                    .ScaleToFit(100, 100)
                    .SetFixedPosition(pdf.GetDefaultPageSize().GetWidth() - 120, pdf.GetDefaultPageSize().GetHeight() - 70);
                document.Add(logo);
            }

            // TÍTULO ALTERADO
            document.Add(new Paragraph("TELE SENA DIGITAL")
                .SetFont(boldFont).SetFontSize(14).SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(20));

            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 4 }))
                .UseAllAvailableWidth().SetMarginBottom(10);
            headerTable.AddCell(new Cell().Add(new Paragraph("OP").SetFont(boldFont)).SetBorder(Border.NO_BORDER));
            headerTable.AddCell(new Cell().Add(new Paragraph("Produto").SetFont(boldFont)).SetBorder(Border.NO_BORDER));
            headerTable.AddCell(new Cell().Add(new Paragraph(relatorio.InfoGeral.OrdemProducao.ToString())).SetBorder(Border.NO_BORDER));
            headerTable.AddCell(new Cell().Add(new Paragraph(relatorio.InfoGeral.Produto)).SetBorder(Border.NO_BORDER));
            document.Add(headerTable);

            // ================== BLOCOS DE INFORMAÇÃO ==================
            Table CreateInfoCell(string label, string value)
            {
                var table = new Table(1).UseAllAvailableWidth();
                table.AddCell(new Cell().Add(new Paragraph(label).SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)).SetBorder(Border.NO_BORDER));
                table.AddCell(new Cell().Add(new Paragraph(value).SetTextAlignment(TextAlignment.CENTER)).SetBorder(Border.NO_BORDER));
                return table;
            }

            var infoRowTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1 }))
                .UseAllAvailableWidth().SetMarginBottom(5).SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
            infoRowTable.AddCell(new Cell().Add(CreateInfoCell("Data de Abertura", relatorio.InfoGeral.DataHoraAbertura.ToString("dd/MM/yyyy HH:mm"))).SetBorder(Border.NO_BORDER));
            infoRowTable.AddCell(new Cell().Add(CreateInfoCell("Data de Fechamento", relatorio.InfoGeral.DataHoraFechamento?.ToString("dd/MM/yyyy HH:mm") ?? "Em Aberto")).SetBorder(Border.NO_BORDER));
            infoRowTable.AddCell(new Cell().Add(CreateInfoCell("Máquina", relatorio.InfoGeral.Maquina)).SetBorder(Border.NO_BORDER));
            document.Add(infoRowTable);

            var motivosExcluidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "AGUARDANDO CAIXA", "AGUARDANDO EMBALAGEM", "AGUARDANDO TURMA",
                "BOBINA DE TESTE", "CIP", "ESTERILIZAÇÃO", "FALTA DE PRODUTO",
                "LIMPEZA INTERMEDIARIA", "LIMPEZA SEMANAL", "LUBRIFICAÇÃO",
                "MANUTENÇÃO PREVENTIVA"
            };

            TimeSpan horasProdutivas = TimeSpan.Zero;
            TimeSpan horasTotais = TimeSpan.Zero;

            foreach (var parada in relatorio.Paradas)
            {
                TimeSpan ts = TimeSpan.Zero;
                if (!string.IsNullOrEmpty(parada.Tempo))
                    TimeSpan.TryParse(parada.Tempo, out ts);

                if ("PRODUÇÃO".Equals(parada.Motivo, StringComparison.OrdinalIgnoreCase))
                {
                    horasProdutivas += ts;
                }

                if (!motivosExcluidos.Contains(parada.Motivo))
                {
                    horasTotais += ts;
                }
            }

            string eficienciaPorTempoStr = "N/A";
            if (horasTotais.TotalSeconds > 0)
            {
                double eficienciaPercentual = (horasProdutivas.TotalSeconds / horasTotais.TotalSeconds);
                eficienciaPorTempoStr = eficienciaPercentual.ToString("P1");
            }

            int totalProcessadas = relatorio.Detalhamentos.Sum(d => d.EmbProcessadas);
            int totalProduzidas = relatorio.Detalhamentos.Sum(d => d.EmbProduzidas);
            int totalPerdidas = relatorio.Detalhamentos.Sum(d => d.EmbPerdidas);
            string perdasStr = totalProcessadas > 0 ? $"({(double)totalPerdidas / totalProcessadas:P1})" : "";

            var embalagensTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1, 1 }))
                .UseAllAvailableWidth().SetMarginBottom(20).SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
            embalagensTable.AddCell(new Cell().Add(CreateInfoCell("Embalagens Processadas", totalProcessadas.ToString())).SetBorder(Border.NO_BORDER));
            embalagensTable.AddCell(new Cell().Add(CreateInfoCell("Embalagens Produzidas", totalProduzidas.ToString())).SetBorder(Border.NO_BORDER));
            embalagensTable.AddCell(new Cell().Add(CreateInfoCell("Embalagens Perdidas", $"{totalPerdidas} {perdasStr}")).SetBorder(Border.NO_BORDER));
            embalagensTable.AddCell(new Cell().Add(CreateInfoCell("Eficiência", eficienciaPorTempoStr)).SetBorder(Border.NO_BORDER));
            document.Add(embalagensTable);

            // ================== PERFORMANCE POR OPERADOR ==================
            var porOperador = relatorio.Detalhamentos
                .GroupBy(d => string.IsNullOrWhiteSpace(d.Operador) ? "Desconhecido" : d.Operador)
                .Select(g => new
                {
                    Operador = g.Key,
                    Processadas = g.Sum(x => x.EmbProcessadas),
                    Produzidas = g.Sum(x => x.EmbProduzidas),
                    Perdidas = g.Sum(x => x.EmbPerdidas)
                })
                .OrderBy(x => x.Operador)
                .ToList();

            document.Add(new Paragraph("Performance por Operador")
                .SetFont(boldFont).SetFontSize(12).SetMarginBottom(5));

            var tableOperador = new Table(UnitValue.CreatePercentArray(new float[] { 2, 1, 1, 1, 1, 1 }))
                .UseAllAvailableWidth().SetMarginBottom(20);
            tableOperador.AddHeaderCell(new Cell().Add(new Paragraph("Operador").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));
            tableOperador.AddHeaderCell(new Cell().Add(new Paragraph("Processadas").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));
            tableOperador.AddHeaderCell(new Cell().Add(new Paragraph("Produzidas").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));
            tableOperador.AddHeaderCell(new Cell().Add(new Paragraph("Perdidas").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));
            tableOperador.AddHeaderCell(new Cell().Add(new Paragraph("% Perda").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));
            tableOperador.AddHeaderCell(new Cell().Add(new Paragraph("Eficiência").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));

            foreach (var opData in porOperador)
            {
                var opEficiencia = opData.Processadas > 0 ? ((double)opData.Produzidas / opData.Processadas) * 100 : 0;
                var opPercPerda = opData.Processadas > 0 ? ((double)opData.Perdidas / opData.Processadas) * 100 : 0;

                tableOperador.AddCell(new Cell().Add(new Paragraph(opData.Operador).SetTextAlignment(TextAlignment.CENTER)));
                tableOperador.AddCell(new Cell().Add(new Paragraph($"{opData.Processadas}").SetTextAlignment(TextAlignment.CENTER)));
                tableOperador.AddCell(new Cell().Add(new Paragraph($"{opData.Produzidas}").SetTextAlignment(TextAlignment.CENTER)));
                tableOperador.AddCell(new Cell().Add(new Paragraph($"{opData.Perdidas}").SetTextAlignment(TextAlignment.CENTER)));
                tableOperador.AddCell(new Cell().Add(new Paragraph($"{opPercPerda:F1}%").SetTextAlignment(TextAlignment.CENTER)));
                tableOperador.AddCell(new Cell().Add(new Paragraph($"{opEficiencia:F1}%").SetTextAlignment(TextAlignment.CENTER)));
            }
            document.Add(tableOperador);

            // ================== TABELAS DE DETALHAMENTO ==================
            var sideBySideTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth().SetMarginBottom(10);

            var perdasTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1 })).UseAllAvailableWidth();
            perdasTable.AddHeaderCell(new Cell(1, 2).Add(new Paragraph("Detalhamento de Perdas").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)).SetBorderBottom(new SolidBorder(1)));
            perdasTable.AddHeaderCell(new Cell().Add(new Paragraph("Motivo").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)).SetBorder(Border.NO_BORDER));
            perdasTable.AddHeaderCell(new Cell().Add(new Paragraph("Quantidade").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)).SetBorder(Border.NO_BORDER));
            if (relatorio.Perdas.Any())
            {
                foreach (var perda in relatorio.Perdas)
                {
                    perdasTable.AddCell(new Cell().Add(new Paragraph(perda.Motivo)).SetBorder(Border.NO_BORDER));
                    perdasTable.AddCell(new Cell().Add(new Paragraph(perda.Quantidade.ToString())).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT));
                }
            }
            else
            {
                perdasTable.AddCell(new Cell(1, 2).Add(new Paragraph("Nenhum registro de perda.").SetFont(italicFont)).SetBorder(Border.NO_BORDER));
            }
            sideBySideTable.AddCell(new Cell().Add(perdasTable).SetBorder(Border.NO_BORDER).SetPaddingRight(10));

            var paradasTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1 })).UseAllAvailableWidth();
            paradasTable.AddHeaderCell(new Cell(1, 2).Add(new Paragraph("Detalhamento de Paradas").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)).SetBorderBottom(new SolidBorder(1)));
            paradasTable.AddHeaderCell(new Cell().Add(new Paragraph("Motivo").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)).SetBorder(Border.NO_BORDER));
            paradasTable.AddHeaderCell(new Cell().Add(new Paragraph("Tempo").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)).SetBorder(Border.NO_BORDER));
            if (relatorio.Paradas.Any())
            {
                foreach (var parada in relatorio.Paradas)
                {
                    string tempoStr = !string.IsNullOrEmpty(parada.Tempo) ? parada.Tempo.Substring(0, 5) : "00:00";
                    paradasTable.AddCell(new Cell().Add(new Paragraph(parada.Motivo)).SetBorder(Border.NO_BORDER));
                    paradasTable.AddCell(new Cell().Add(new Paragraph(tempoStr)).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT));
                }
            }
            else
            {
                paradasTable.AddCell(new Cell(1, 2).Add(new Paragraph("Nenhum registro de parada.").SetFont(italicFont)).SetBorder(Border.NO_BORDER));
            }
            sideBySideTable.AddCell(new Cell().Add(paradasTable).SetBorder(Border.NO_BORDER).SetPaddingLeft(10));
            document.Add(sideBySideTable);

            // ================== TABELA DE PALETIZAÇÃO ==================
            document.Add(new Paragraph("Detalhamento de Paletização").SetFont(boldFont).SetFontSize(12).SetMarginTop(15));
            var paleteTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1.5f, 1, 2, 2 }))
                .UseAllAvailableWidth().SetMarginTop(5);
            paleteTable.AddHeaderCell(new Cell().Add(new Paragraph("N° Palete").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));
            paleteTable.AddHeaderCell(new Cell().Add(new Paragraph("Quantidade por Palete").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));
            paleteTable.AddHeaderCell(new Cell().Add(new Paragraph("Unidade").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));
            paleteTable.AddHeaderCell(new Cell().Add(new Paragraph("Usuário").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));
            paleteTable.AddHeaderCell(new Cell().Add(new Paragraph("Data/Hora").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));

            if (relatorio.Paletes.Any())
            {
                int totalQtdePalete = 0;
                string unidade = relatorio.Paletes.First().Unidade ?? "";

                foreach (var palete in relatorio.Paletes.OrderBy(p => p.N_Palete))
                {
                    paleteTable.AddCell(new Cell().Add(new Paragraph(palete.N_Palete.ToString()).SetTextAlignment(TextAlignment.CENTER)));
                    paleteTable.AddCell(new Cell().Add(new Paragraph(palete.QtdePorPalete.ToString()).SetTextAlignment(TextAlignment.CENTER)));
                    paleteTable.AddCell(new Cell().Add(new Paragraph(palete.Unidade ?? "-").SetTextAlignment(TextAlignment.CENTER)));
                    paleteTable.AddCell(new Cell().Add(new Paragraph(palete.Usuario).SetTextAlignment(TextAlignment.CENTER)));
                    paleteTable.AddCell(new Cell().Add(new Paragraph(palete.DataHoraPaletizacao.ToString("dd/MM/yyyy HH:mm")).SetTextAlignment(TextAlignment.CENTER)));

                    totalQtdePalete += palete.QtdePorPalete;
                }

                paleteTable.AddCell(new Cell(1, 1).Add(new Paragraph("Total:").SetFont(boldFont).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(Border.NO_BORDER));
                paleteTable.AddCell(new Cell(1, 4).Add(new Paragraph($"{totalQtdePalete} {unidade}").SetFont(boldFont)).SetBorder(Border.NO_BORDER));
            }
            else
            {
                paleteTable.AddCell(new Cell(1, 5).Add(new Paragraph("Nenhum palete registrado.").SetFont(italicFont).SetTextAlignment(TextAlignment.CENTER)));
            }
            document.Add(paleteTable);

            // ================== RODAPÉ ==================
            var operadores = string.Join(", ", relatorio.Detalhamentos.Select(d => d.Operador).Distinct());
            var pistoladores = string.Join(", ", relatorio.Paletes.Select(p => p.Usuario).Distinct());

            document.Add(new Paragraph($"Operador(es): {operadores}")
                .SetMarginTop(30));
            document.Add(new Paragraph($"Pistolador(es): {pistoladores}"));

            string dataFechamento = relatorio.InfoGeral.DataHoraFechamento?.ToString("dd/MM/yyyy HH:mm") ?? "Em aberto";
            string nomeSupervisor = string.IsNullOrEmpty(relatorio.InfoGeral.SupervisorFechamento) 
                ? "___________________________" 
                : relatorio.InfoGeral.SupervisorFechamento;

            document.Add(new Paragraph($"Ordem de Produção revisada e fechada por {nomeSupervisor} (Supervisor) em {dataFechamento}")
                .SetMarginTop(20).SetFont(boldFont));

            document.Close();
            return memoryStream.ToArray();
        }
    }
}
