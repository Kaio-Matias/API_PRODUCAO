using API_PRODUCAO.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_PRODUCAO.Services.Interfaces
{
    public interface IProducaoService
    {
        Task<Producoes> CreateProducaoAsync(Producoes producao);
        Task<Producoes?> GetProducaoByIdAsync(int id);
        Task<IEnumerable<Producoes>> GetAllProducoesAsync();
        Task<IEnumerable<Producoes>> GetProducoesAbertasAsync();
        Task<bool> FecharProducaoAsync(int ordemProducao);
        Task<IEnumerable<Producoes>> GetProducoesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Producoes>> GetClosedProducoesByDateRangeAsync(DateTime startDate, DateTime endDate);

        // ===== NOVO MÉTODO ADICIONADO =====
        /// <summary>
        /// Altera o status de uma Ordem de Produção para "Cancelado".
        /// </summary>
        /// <param name="ordemProducao">O número da OP a ser cancelada.</param>
        /// <returns>Retorna 'true' se a operação foi bem-sucedida.</returns>
        Task<bool> CancelarProducaoAsync(int ordemProducao);
    }
}