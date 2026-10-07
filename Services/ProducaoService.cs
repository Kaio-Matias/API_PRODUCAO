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
        /// Vincula automaticamente o A Granel correspondente ao produto, caso exista
        /// um registro em VinculosAgranelAcabado com o código do produto.
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
        /// Altera o status de uma Ordem de Produção para "Fechado", registra a data/hora do
        /// fechamento e, se houver vínculo com A Granel, debita automaticamente o consumo
        /// de EstoqueAgranel e gera um movimento do tipo CONSUMO no histórico.
        /// </summary>
        /// <param name="ordemProducao">O número da OP a ser fechada.</param>
        /// <param name="supervisor">O nome do supervisor que fechou a OP.</param>
        /// <returns>Retorna 'true' se a operação foi bem-sucedida, e 'false' se a OP não foi encontrada ou já estava fechada.</returns>
        public async Task<bool> FecharProducaoAsync(int ordemProducao, string? supervisor = null)
        {
            // Carrega a OP junto com todos os DetalhamentoOPs para calcular o consumo
            var producao = await _context.Producoes
                .Include(p => p.DetalhamentoOPs)
                .FirstOrDefaultAsync(p => p.OrdemProducao == ordemProducao);

            if (producao == null || producao.Status != "Aberto")
                return false;

            producao.Status = "Fechado";
            producao.DataHoraFechamento = DateTime.Now;
            producao.SupervisorFechamento = supervisor;

            // ── Débito automático de A Granel ──────────────────────────────────────
            // Calcula o consumo somente quando a OP tem vínculo e fator configurados
            if (!string.IsNullOrEmpty(producao.CodigoAgranel) && producao.FatorConversaoLiters > 0)
            {
                int totalEmb = producao.DetalhamentoOPs?.Sum(d => d.EmbProduzidas) ?? 0;

                if (totalEmb > 0)
                {
                    double consumoLitros = totalEmb * producao.FatorConversaoLiters;

                    var estoque = await _context.EstoqueAgranel
                        .FirstOrDefaultAsync(e => e.CodigoAgranel == producao.CodigoAgranel);

                    double saldoAnterior = estoque?.SaldoLitros ?? 0;
                    double saldoNovo     = saldoAnterior - consumoLitros;

                    if (estoque == null)
                    {
                        // Cria o registro de estoque se ainda não existir (saldo negativo indica
                        // que o produto foi consumido antes de qualquer entrada ser registrada)
                        var vinculo = await _context.VinculosAgranelAcabado
                            .FirstOrDefaultAsync(v => v.CodigoAgranel == producao.CodigoAgranel);

                        _context.EstoqueAgranel.Add(new EstoqueAgranel
                        {
                            CodigoAgranel    = producao.CodigoAgranel,
                            DescricaoAgranel = vinculo?.DescricaoAgranel ?? producao.CodigoAgranel,
                            SaldoLitros      = saldoNovo,
                            ValorUnitario    = 0,
                            UltimaAtualizacao = DateTime.Now
                        });
                    }
                    else
                    {
                        estoque.SaldoLitros       = saldoNovo;
                        estoque.UltimaAtualizacao = DateTime.Now;
                    }

                    // Registra o movimento no histórico imutável
                    await _context.MovimentosEstoqueAgranel.AddAsync(new MovimentoEstoqueAgranel
                    {
                        CodigoAgranel    = producao.CodigoAgranel,
                        TipoMovimento    = "CONSUMO",
                        QuantidadeLitros = -consumoLitros,
                        SaldoAnterior    = saldoAnterior,
                        SaldoPosterior   = saldoNovo,
                        DataMovimento    = DateTime.Now,
                        Referencia       = $"OP #{ordemProducao}",
                        Observacao       = $"Consumo automático: {totalEmb} emb × {producao.FatorConversaoLiters:F4} L/emb = {consumoLitros:F2} L"
                    });
                }
            }
            // ──────────────────────────────────────────────────────────────────────

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

        public async Task<Producoes?> UpdateProducaoAsync(int ordemProducao, Producoes newData)
        {
            var existing = await _context.Producoes.FirstOrDefaultAsync(p => p.OrdemProducao == ordemProducao);
            if (existing == null) return null;

            existing.Produto = newData.Produto;
            existing.Maquina = newData.Maquina;
            existing.Unidade = newData.Unidade;
            existing.Status = newData.Status;
            existing.CodigoAgranel = newData.CodigoAgranel;
            existing.FatorConversaoLiters = newData.FatorConversaoLiters;
            existing.DataHoraAbertura = newData.DataHoraAbertura;
            if (newData.DataHoraFechamento.HasValue)
            {
                existing.DataHoraFechamento = newData.DataHoraFechamento;
            } else {
                existing.DataHoraFechamento = null;
            }

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteProducaoAsync(int ordemProducao)
        {
            var producao = await _context.Producoes
                .Include(p => p.DetalhamentoOPs)
                .Include(p => p.Perdas)
                .Include(p => p.Eficiencia)
                .Include(p => p.Paletizacoes)
                .FirstOrDefaultAsync(p => p.OrdemProducao == ordemProducao);

            if (producao == null) return false;

            _context.DetalhamentoOPs.RemoveRange(producao.DetalhamentoOPs);
            _context.Perdas.RemoveRange(producao.Perdas);
            _context.Eficiencia.RemoveRange(producao.Eficiencia);
            _context.Paletizacoes.RemoveRange(producao.Paletizacoes);

            _context.Producoes.Remove(producao);
            
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