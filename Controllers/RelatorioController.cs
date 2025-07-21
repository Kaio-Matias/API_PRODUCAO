using Valedourado.Shared.Dtos;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API_PRODUCAO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RelatorioController : ControllerBase
    {
        private readonly IRelatorioService _relatorioService;

        public RelatorioController(IRelatorioService relatorioService)
        {
            _relatorioService = relatorioService;
        }

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
        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardDto>> GetDashboardData()
        {
            var dashboardData = await _relatorioService.GetDashboardDataAsync();
            return Ok(dashboardData);
        }
        [HttpGet("op/{ordemProducao}/pdf")]
        public async Task<IActionResult> GetRelatorioPdf(int ordemProducao)
        {
            try
            {
                var pdfBytes = await _relatorioService.GerarPdfDeRelatorioAsync(ordemProducao);
                string fileName = $"Relatorio_OP_{ordemProducao}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}