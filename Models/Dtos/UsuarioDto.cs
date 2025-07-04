using System.ComponentModel.DataAnnotations;

namespace API_PRODUCAO.DTOs
{
    public class CreateUsuarioDto
    {
        [Required]
        public string? Nome { get; set; }
        public string? Cargo { get; set; }
        [Required]
        public int? Matricula { get; set; }
    }

    public class UsuarioDto
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public string? Cargo { get; set; }
        public int? Matricula { get; set; }
    }

    public class LoginRequestDto
    {
        [Required]
        public int? Matricula { get; set; }
    }
}