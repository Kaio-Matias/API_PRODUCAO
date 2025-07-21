using Valedourado.Shared.Dtos;

namespace API_PRODUCAO.Services.Interfaces
{
    public interface IRelatorioService
    {
        Task<RelatorioOpCompletoDto?> GetRelatorioCompletoOpAsync(int ordemProducao);
        Task<byte[]> GerarPdfDeRelatorioAsync(int ordemProducao);
        Task<DashboardDto> GetDashboardDataAsync();
    }
}