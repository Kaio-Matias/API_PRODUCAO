using System.ComponentModel.DataAnnotations;

namespace API_PRODUCAO.Models
{
    public class ColetaLeite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(40)]
        public string NumeroColeta { get; set; } = string.Empty;

        public DateTime DataHoraColeta { get; set; }

        [Required]
        [MaxLength(10)]
        public string PlacaCaminhao { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Transportador { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Linha { get; set; } = string.Empty;

        /// <summary>Produtor(es) da viagem; se houver mais de um, separados por vírgula.</summary>
        [MaxLength(300)]
        public string Produtor { get; set; } = string.Empty;

        public double VolumeDescarregado { get; set; }

        public double VolumeDetalhado { get; set; }
    }
}
