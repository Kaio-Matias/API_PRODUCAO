using API_PRODUCAO.Models;

namespace API_PRODUCAO.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<Usuarios?> LoginAsync(int matricula);
        Task<Usuarios> RegisterAsync(Usuarios usuario);
        Task<Usuarios?> GetUsuarioByIdAsync(int id);
        Task<bool> UsuarioExistsAsync(int matricula);
        Task<IEnumerable<Usuarios>> GetTodosUsuariosAsync();
        Task<Usuarios?> UpdateModulosAcessoAsync(int id, string modulosAcessoJson);
        Task<bool> DeleteAsync(int id);
    }
}