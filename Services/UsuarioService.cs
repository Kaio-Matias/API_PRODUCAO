using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using API_PRODUCAO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_PRODUCAO.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly AppDbContext _context;

        public UsuarioService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Usuarios?> LoginAsync(int matricula)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Matricula == matricula);
        }

        public async Task<Usuarios> RegisterAsync(Usuarios usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<Usuarios?> GetUsuarioByIdAsync(int id)
        {
            return await _context.Usuarios.FindAsync(id);
        }

        public async Task<bool> UsuarioExistsAsync(int matricula)
        {
            return await _context.Usuarios.AnyAsync(u => u.Matricula == matricula);
        }

        public async Task<IEnumerable<Usuarios>> GetTodosUsuariosAsync()
        {
            return await _context.Usuarios.ToListAsync();
        }

        public async Task<Usuarios?> UpdateModulosAcessoAsync(int id, string modulosAcessoJson)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return null;

            usuario.ModulosAcesso = modulosAcessoJson;
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return false;

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}