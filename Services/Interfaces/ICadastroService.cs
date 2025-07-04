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
    }
}