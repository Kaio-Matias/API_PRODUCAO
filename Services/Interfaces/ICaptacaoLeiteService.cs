using API_PRODUCAO.Models;

namespace API_PRODUCAO.Services.Interfaces
{
    public interface ICaptacaoLeiteService
    {
        Task<IEnumerable<CaptacaoLeite>> GetByMesAnoAsync(int mes, int ano);
        Task<CaptacaoLeite> CreateAsync(CaptacaoLeite captacao);
        Task<CaptacaoLeite?> UpdateAsync(int id, CaptacaoLeite captacao);
        Task<bool> DeleteAsync(int id);
    }
}
