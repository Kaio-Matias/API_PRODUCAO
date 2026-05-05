using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_PRODUCAO.Models
{
    public class IndicadorAgranel
    {
        [Key]
        public int Id { get; set; }

        public DateTime DataReferencia { get; set; }

        [Required]
        [MaxLength(20)]
        public string CodigoAgranel { get; set; } = string.Empty;

        public string DescricaoAgranel { get; set; } = string.Empty;

        public double Inicial { get; set; }
        public double Preparado { get; set; }
        public double Consumo { get; set; }
        public double Final { get; set; }
        
        public double Perdas { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal ValorUnitario { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PerdaValorizada { get; set; }
        
        public double PercentualPerda { get; set; }
        
        public string? Observacoes { get; set; }
    }
}
