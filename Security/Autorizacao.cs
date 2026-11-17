using System.Security.Claims;
using System.Text.Json;
using API_PRODUCAO.Data;
using Microsoft.AspNetCore.Authorization;

namespace API_PRODUCAO.Security
{
    /// <summary>Nomes das políticas usadas nos controllers.</summary>
    public static class Politicas
    {
        /// <summary>Somente Supervisor/Admin: usuários, exclusões e operações destrutivas.</summary>
        public const string Administracao = "Administracao";

        /// <summary>Supervisor/Admin ou quem tem o módulo de abertura de OP.</summary>
        public const string AberturaOp = "AberturaOp";

        /// <summary>Supervisor/Admin ou quem tem o módulo de gestão de OP (fechar, cancelar, editar).</summary>
        public const string GestaoOp = "GestaoOp";
    }

    public static class Cargos
    {
        public const string Operador    = "Operador";
        public const string Supervisor  = "Supervisor";
        public const string Admin       = "Admin";
        public const string Paletizador = "Paletizador";

        public static readonly string[] Todos = { Operador, Supervisor, Admin, Paletizador };

        /// <summary>Normaliza o cargo vindo do banco ("supervisor", "ADMIN"...) para o nome oficial.</summary>
        public static string Normalizar(string? cargo) =>
            Todos.FirstOrDefault(c => c.Equals(cargo?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? Operador;
    }

    /// <summary>
    /// Libera o acesso se o usuário tiver um dos cargos informados OU um dos módulos
    /// liberados no cadastro (coluna ModulosAcesso). Cargo e módulos são lidos do banco a cada
    /// requisição, então alterações feitas pelo admin valem sem novo login.
    /// </summary>
    public class CargoOuModuloRequirement : IAuthorizationRequirement
    {
        public string[] Cargos { get; }
        public string[] Modulos { get; }

        public CargoOuModuloRequirement(string[] cargos, string[] modulos)
        {
            Cargos  = cargos;
            Modulos = modulos;
        }
    }

    public class CargoOuModuloHandler : AuthorizationHandler<CargoOuModuloRequirement>
    {
        private readonly AppDbContext _db;

        public CargoOuModuloHandler(AppDbContext db) => _db = db;

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, CargoOuModuloRequirement req)
        {
            if (context.User.Identity?.IsAuthenticated != true) return;
            if (!int.TryParse(context.User.FindFirst("id")?.Value, out var id)) return;

            // Usa o cadastro atual (o usuário pode ter sido excluído ou rebaixado depois do login)
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return;

            if (req.Cargos.Any(c => c.Equals(usuario.Cargo?.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(req);
                return;
            }

            if (req.Modulos.Length == 0) return;

            try
            {
                var modulos = JsonSerializer.Deserialize<List<string>>(usuario.ModulosAcesso ?? "[]") ?? new();
                if (modulos.Any(m => req.Modulos.Contains(m, StringComparer.OrdinalIgnoreCase)))
                    context.Succeed(req);
            }
            catch (JsonException)
            {
                // ModulosAcesso inválido = sem módulos extras
            }
        }
    }
}
