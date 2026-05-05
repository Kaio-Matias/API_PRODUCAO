namespace API_PRODUCAO.Models
{
    public class Usuarios
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public string? Cargo { get; set; }
        public int? Matricula { get; set; }
        public string ModulosAcesso { get; set; } = "[]";
    }
}