using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_PRODUCAO.Services
{
    /// <summary>
    /// Serviço responsável por toda a lógica de negócio relacionada às Ordens de Produção.
    /// </summary>
    public class ProducaoService : IProducaoService
    {
        private readonly AppDbContext _context;

        public ProducaoService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cria uma nova Ordem de Produção no banco de dados.
        /// Define o status como "Aberto" e a data/hora de abertura para o momento atual.
        /// </summary>
        /// <param name="producao">O objeto de produção a ser criado.</param>
        /// <returns>A entidade de produção após ser salva no banco.</returns>
        public async Task<Producoes> CreateProducaoAsync(Producoes producao)
        {
            producao.Status = "Aberto";
            producao.DataHoraAbertura = DateTime.Now;
            _context.Producoes.Add(producao);
            await _context.SaveChangesAsync();
            return producao;
        }

        /// <summary>
        /// Busca todas as Ordens de Produção, independentemente do status.
        /// </summary>
        /// <returns>Uma lista de todas as produções.</returns>
        public async Task<IEnumerable<Producoes>> GetAllProducoesAsync()
        {
            return await _context.Producoes.ToListAsync();
        }

        /// <summary>
        /// Busca uma Ordem de Produção específica pelo seu número (chave primária).
        /// </summary>
        /// <param name="id">O número da Ordem de Produção.</param>
        /// <returns>A entidade de produção encontrada ou nulo.</returns>
        public async Task<Producoes?> GetProducaoByIdAsync(int id)
        {
            return await _context.Producoes.FirstOrDefaultAsync(p => p.OrdemProducao == id);
        }

        /// <summary>
        /// Busca todas as Ordens de Produção que possuem o status "Aberto".
        /// </summary>
        /// <returns>Uma lista de produções abertas.</returns>
        public async Task<IEnumerable<Producoes>> GetProducoesAbertasAsync()
        {
            return await _context.Producoes
                .Where(p => p.Status == "Aberto")
                .ToListAsync();
        }

        /// <summary>
        /// Altera o status de uma Ordem de Produção para "Fechado" e registra a data/hora do fechamento.
        /// </summary>
        /// <param name="ordemProducao">O número da OP a ser fechada.</param>
        /// <returns>Retorna 'true' se a operação foi bem-sucedida, e 'false' se a OP não foi encontrada ou já estava fechada.</returns>
        public async Task<bool> FecharProducaoAsync(int ordemProducao)
        {
            var producao = await _context.Producoes
                .FirstOrDefaultAsync(p => p.OrdemProducao == ordemProducao);

            if (producao == null || producao.Status != "Aberto")
            {
                // Retorna false se a OP não for encontrada ou se seu status não for "Aberto"
                return false;
            }

            producao.Status = "Fechado";
            producao.DataHoraFechamento = DateTime.Now;

            // Salva as alterações no banco de dados
            await _context.SaveChangesAsync();

            return true;
        }

        // ===== NOVO MÉTODO ADICIONADO =====
        /// <summary>
        /// Altera o status de uma Ordem de Produção para "Cancelado" e registra a data/hora.
        /// </summary>
        /// <param name="ordemProducao">O número da OP a ser cancelada.</param>
        /// <returns>Retorna 'true' se a operação foi bem-sucedida, e 'false' se a OP não foi encontrada ou já estava fechada/cancelada.</returns>
        public async Task<bool> CancelarProducaoAsync(int ordemProducao)
        {
            var producao = await _context.Producoes
                .FirstOrDefaultAsync(p => p.OrdemProducao == ordemProducao);

            // Não pode cancelar uma OP que não existe ou que não está "Aberto"
            if (producao == null || producao.Status != "Aberto")
            {
                return false;
            }

            producao.Status = "Cancelado";
            producao.DataHoraFechamento = DateTime.Now; // Registra quando foi cancelada

            await _context.SaveChangesAsync();
            return true;
        }
        // ===================================

        public async Task<IEnumerable<Producoes>> GetProducoesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            // Garante que o filtro inclua o dia final inteiro (até 23:59:59)
            var finalEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            return await _context.Producoes
                .Where(p => p.DataHoraAbertura >= startDate.Date && p.DataHoraAbertura <= finalEndDate)
                .OrderByDescending(p => p.OrdemProducao)
                .ToListAsync();
        }

        public async Task<IEnumerable<Producoes>> GetClosedProducoesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var finalEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            return await _context.Producoes
                .Where(p => p.Status == "Fechado" &&
                             p.DataHoraAbertura.Date >= startDate.Date &&
                             p.DataHoraAbertura.Date <= finalEndDate)
                .OrderByDescending(p => p.OrdemProducao)
                .ToListAsync();
        }
    }
}