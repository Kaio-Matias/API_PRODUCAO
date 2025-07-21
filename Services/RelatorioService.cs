using API_PRODUCAO.Data;
using API_PRODUCAO.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Borders;
using Valedourado.Shared.Dtos; // Usando APENAS o namespace da biblioteca compartilhada

// Usando aliases para resolver ambiguidades com a biblioteca iText
using Document = iText.Layout.Document;
using Image = iText.Layout.Element.Image;
using Paragraph = iText.Layout.Element.Paragraph;
using Table = iText.Layout.Element.Table;
using Cell = iText.Layout.Element.Cell;
using TextAlignment = iText.Layout.Properties.TextAlignment;
using UnitValue = iText.Layout.Properties.UnitValue;
using HorizontalAlignment = iText.Layout.Properties.HorizontalAlignment;

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

        // A lógica do GetDashboardDataAsync precisa das propriedades de navegação
        // que adicionamos nos modelos do banco.
        public async Task<DashboardDto> GetDashboardDataAsync()
        {
            var hoje = DateTime.Today;

            var opsAbertas = await _context.Producoes.CountAsync(p => p.Status == "Aberto");

            var totalProduzido = await _context.DetalhamentoOPs
                .Where(d => d.Producao.DataHoraAbertura.Date == hoje)
                .SumAsync(d => d.EmbProduzidas);

            var totalPerdido = await _context.Perdas
                .Where(p => p.Producao.DataHoraAbertura.Date == hoje)
                .SumAsync(p => p.Quantidade);

            return new DashboardDto
            {
                OpsAbertas = opsAbertas,
                TotalProduzidoHoje = totalProduzido,
                TotalPerdidoHoje = totalPerdido
            };
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
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
            document.SetMargins(30, 30, 30, 30);

            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            string imagePath = Path.Combine("wwwroot", "img", "logo.png");
            if (File.Exists(imagePath))
            {
                var logo = new Image(ImageDataFactory.Create(imagePath)).ScaleToFit(80, 80).SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                document.Add(logo);
            }

            document.Add(new Paragraph("Relatório de Ordem de Produção")
                .SetFont(boldFont).SetFontSize(18).SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(20));

            var infoGeral = relatorio.InfoGeral;
            string dataFechamentoStr = infoGeral.DataHoraFechamento?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";

            document.Add(new Paragraph("Informações Gerais").SetFont(boldFont).SetFontSize(14));
            document.Add(new Paragraph($"OP: {infoGeral.OrdemProducao} | Status: {infoGeral.Status}").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph($"Produto: {infoGeral.Produto} | Máquina: {infoGeral.Maquina} | Unidade: {infoGeral.Unidade}").SetFont(font).SetFontSize(10));
            document.Add(new Paragraph($"Abertura: {infoGeral.DataHoraAbertura:dd/MM/yyyy HH:mm} | Fechamento: {dataFechamentoStr}").SetFont(font).SetFontSize(10).SetMarginBottom(15));

            if (relatorio.Perdas.Any())
            {
                document.Add(new Paragraph("Registros de Perdas").SetFont(boldFont).SetFontSize(14).SetMarginTop(10));
                var perdasTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1, 2 })).UseAllAvailableWidth();
                perdasTable.AddHeaderCell(new Cell().Add(new Paragraph("Motivo").SetFont(boldFont)));
                perdasTable.AddHeaderCell(new Cell().Add(new Paragraph("Quantidade").SetFont(boldFont)));
                perdasTable.AddHeaderCell(new Cell().Add(new Paragraph("Operador").SetFont(boldFont)));
                foreach (var perda in relatorio.Perdas)
                {
                    perdasTable.AddCell(perda.Motivo ?? "");
                    perdasTable.AddCell(perda.Quantidade.ToString());
                    perdasTable.AddCell(perda.Operador ?? "");
                }
                document.Add(perdasTable.SetMarginBottom(15));
            }

            document.Close();
            return memoryStream.ToArray();
        }
    }
}