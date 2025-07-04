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
    }
}