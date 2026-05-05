using API_PRODUCAO.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_PRODUCAO.Services.Interfaces
{
    public interface ICadastroService
    {
        Task<IEnumerable<Cadastro>> GetAllAsync();
        Task<Cadastro?> GetByCodProdutoAsync(string codProduto);
        Task<Cadastro?> GetByCodBarraAsync(string codBarra);
        Task<Cadastro> CreateAsync(Cadastro cadastro);
        Task<Cadastro?> UpdateAsync(int id, Cadastro cadastro);
        Task<bool> DeleteAsync(int id);
    }
}