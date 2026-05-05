using API_PRODUCAO.Models;

namespace API_PRODUCAO.Services.Interfaces
{
    public interface IMetaProducaoService
    {
        Task<IEnumerable<MetaProducao>> GetMetasByMesAnoAsync(int mes, int ano);
        Task<MetaProducao?> GetMetaByClasseAsync(int mes, int ano, string classe);
        Task<MetaProducao> UpsertMetaAsync(MetaProducao meta);
        Task<bool> DeleteMetaAsync(int id);

        Task<MetaCaptacaoLeite?> GetMetaCaptacaoAsync(int mes, int ano);
        Task<MetaCaptacaoLeite> UpsertMetaCaptacaoAsync(MetaCaptacaoLeite meta);
    }
}
