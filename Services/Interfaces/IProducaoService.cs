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
        Task<bool> FecharProducaoAsync(int ordemProducao, string? supervisor = null);
        Task<IEnumerable<Producoes>> GetProducoesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Producoes>> GetClosedProducoesByDateRangeAsync(DateTime startDate, DateTime endDate);

        // ===== NOVO MÉTODO ADICIONADO =====
        /// <summary>
        /// Altera o status de uma Ordem de Produção para "Cancelado".
        /// </summary>
        /// <param name="ordemProducao">O número da OP a ser cancelada.</param>
        /// <returns>Retorna 'true' se a operação foi bem-sucedida.</returns>
        Task<bool> CancelarProducaoAsync(int ordemProducao);

        /// <summary>Reabre OP Fechada/Cancelada e estorna o consumo de A Granel já debitado.</summary>
        Task<bool> ReabrirProducaoAsync(int ordemProducao, string? usuario = null);
        /// <param name="permitirAlterarDatas">true só quando a alteração vem do Portal (data/hora de abertura e fechamento).</param>
        Task<Producoes?> UpdateProducaoAsync(int ordemProducao, Producoes newData, bool permitirAlterarDatas = false);
        Task<bool> DeleteProducaoAsync(int ordemProducao);
    }
}