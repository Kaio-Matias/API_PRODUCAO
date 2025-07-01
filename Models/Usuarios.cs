namespace API_PRODUCAO.Models
{
    public class Usuarios
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public string? Cargo { get; set; } // <-- Garanta que esta linha está aqui e o arquivo está salvo
        public int? Matricula { get; set; }
    }
}