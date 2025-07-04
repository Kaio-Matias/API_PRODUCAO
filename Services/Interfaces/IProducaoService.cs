using API_PRODUCAO.Models;

namespace API_PRODUCAO.Services.Interfaces
{
    public interface IProducaoService
    {
        Task<Producoes> CreateProducaoAsync(Producoes producao);
        Task<Producoes?> GetProducaoByIdAsync(int id);
        Task<IEnumerable<Producoes>> GetAllProducoesAsync();
        Task<IEnumerable<Producoes>> GetProducoesAbertasAsync();
    }
}