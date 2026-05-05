using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_PRODUCAO.Models
{
    /// <summary>
    /// Saldo ATUAL de cada A Granel em LITROS.
    /// Uma linha por código de A Granel — atualizado a cada movimento.
    /// </summary>
    public class EstoqueAgranel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CodigoAgranel { get; set; } = string.Empty;

        [Required]
        public string DescricaoAgranel { get; set; } = string.Empty;

        /// <summary>Saldo atual em LITROS.</summary>
        public double SaldoLitros { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ValorUnitario { get; set; }

        public DateTime UltimaAtualizacao { get; set; } = DateTime.UtcNow;

        public string? Observacoes { get; set; }
    }
}
