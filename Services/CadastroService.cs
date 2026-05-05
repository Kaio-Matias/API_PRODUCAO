using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_PRODUCAO.Services
{
    public class CadastroService : ICadastroService
    {
        private readonly AppDbContext _context;

        public CadastroService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cadastro>> GetAllAsync()
        {
            return await _context.Cadastros.ToListAsync();
        }

        public async Task<Cadastro?> GetByCodBarraAsync(string codBarra)
        {
            return await _context.Cadastros
                .FirstOrDefaultAsync(c => c.CodBarra == codBarra);
        }

        public async Task<Cadastro?> GetByCodProdutoAsync(string codProduto)
        {
            return await _context.Cadastros
                .FirstOrDefaultAsync(c => c.CodProduto == codProduto);
        }

        public async Task<Cadastro> CreateAsync(Cadastro cadastro)
        {
            _context.Cadastros.Add(cadastro);
            await _context.SaveChangesAsync();
            return cadastro;
        }

        public async Task<Cadastro?> UpdateAsync(int id, Cadastro cadastro)
        {
            var existing = await _context.Cadastros.FindAsync(id);
            if (existing == null) return null;

            existing.CodProduto = cadastro.CodProduto;
            existing.Produto = cadastro.Produto;
            existing.CodBarra = cadastro.CodBarra;
            existing.Maquina = cadastro.Maquina;
            existing.Unidade = cadastro.Unidade;
            existing.Classe = cadastro.Classe;
            existing.QtdeCaixa = cadastro.QtdeCaixa;
            existing.QtdePorPalete = cadastro.QtdePorPalete;
            existing.PesoBruto = cadastro.PesoBruto;
            existing.PesoLiquido = cadastro.PesoLiquido;
            existing.PesoTotalPalete = cadastro.PesoTotalPalete;
            existing.PrecoUnitario = cadastro.PrecoUnitario;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.Cadastros.FindAsync(id);
            if (existing == null) return false;

            _context.Cadastros.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}