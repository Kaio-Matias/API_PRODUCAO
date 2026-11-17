using API_PRODUCAO.Models;

namespace API_PRODUCAO.Services.Interfaces
{
    public interface IColetaLeiteService
    {
        Task<IEnumerable<ColetaLeite>> GetByMesAnoAsync(int mes, int ano);
        Task<bool> NumeroExistsAsync(string numeroColeta, int? exceptId = null);
        Task<ColetaLeite> CreateAsync(ColetaLeite coleta);
        Task<ColetaLeite?> UpdateAsync(int id, ColetaLeite coleta);
        Task<bool> DeleteAsync(int id);
    }
}
