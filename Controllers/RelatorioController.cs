using API_PRODUCAO.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Valedourado.Shared.Dtos;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RelatorioController : ControllerBase
    {
        private readonly IRelatorioService _relatorioService;
        private readonly ILogger<RelatorioController> _logger;

        public RelatorioController(IRelatorioService relatorioService, ILogger<RelatorioController> logger)
        {
            _relatorioService = relatorioService;
            _logger = logger;
        }

        /// <summary>
        /// Obtém os dados completos de uma Ordem de Produção para exibição detalhada.
        /// </summary>
        /// <param name="ordemProducao">O número da Ordem de Produção.</param>
        /// <returns>Um relatório completo com todas as informações da OP.</returns>
        [HttpGet("op/{ordemProducao}")]
        public async Task<ActionResult<RelatorioOpCompletoDto>> GetRelatorioCompleto(int ordemProducao)
        {
            var relatorio = await _relatorioService.GetRelatorioCompletoOpAsync(ordemProducao);
            if (relatorio == null)
            {
                return NotFound($"Nenhum dado encontrado para a OP: {ordemProducao}");
            }
            return Ok(relatorio);
        }

        /// <summary>
        /// Obtém os principais indicadores (KPIs) para o dashboard do supervisor.
        /// </summary>
        /// <returns>Os dados consolidados para o dashboard.</returns>
        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardDto>> GetDashboardData()
        {
            try
            {
                var dashboardData = await _relatorioService.GetDashboardDataAsync();
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar dados do dashboard.");
                return StatusCode(500, "Ocorreu um erro interno ao buscar os dados do dashboard.");
            }
        }

        /// <summary>
        /// Gera e retorna um relatório em PDF para uma Ordem de Produção específica.
        /// </summary>
        /// <param name="ordemProducao">O número da Ordem de Produção.</param>
        /// <returns>Um arquivo PDF.</returns>
        [HttpGet("op/{ordemProducao}/pdf")]
        public async Task<IActionResult> GetRelatorioPdf(int ordemProducao)
        {
            try
            {
                var pdfBytes = await _relatorioService.GerarPdfDeRelatorioAsync(ordemProducao);
                string fileName = $"Relatorio_OP_{ordemProducao}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (FileNotFoundException ex)
            {
                // Este erro é lançado pelo serviço se a OP não for encontrada.
                // Aqui, um 404 (Não Encontrado) faz todo o sentido.
                _logger.LogWarning(ex, "Tentativa de gerar PDF para OP {OrdemProducao} que não foi encontrada.", ordemProducao);
                return NotFound($"Os dados para a Ordem de Produção {ordemProducao} não foram encontrados.");
            }
            catch (Exception ex)
            {
                // Qualquer outro erro (imagem faltando, falha na biblioteca iText, etc.) é um erro de servidor.
                // Registramos o erro real para nós e retornamos um erro 500 para o cliente.
                _logger.LogError(ex, "Erro inesperado ao gerar PDF para OP {OrdemProducao}.", ordemProducao);
                return StatusCode(500, "Ocorreu um erro interno no servidor ao gerar o PDF.");
            }
        }
    }
}