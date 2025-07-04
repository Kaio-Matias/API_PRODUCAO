using System.ComponentModel.DataAnnotations;

namespace API_PRODUCAO.DTOs
{
    public class CreateProducaoDto
    {
        [Required]
        public string? Produto { get; set; }
        public string? Maquina { get; set; }
        [Required]
        public string? Unidade { get; set; }
    }

    public class ProducaoDto
    {
        public int OrdemProducao { get; set; }
        public string? Produto { get; set; }
        public string? Maquina { get; set; }
        public string? Unidade { get; set; }
        public string? Status { get; set; }
        public DateTime DataHoraAbertura { get; set; }
        public DateTime? DataHoraFechamento { get; set; }
    }
}