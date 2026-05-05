using System.ComponentModel.DataAnnotations;

namespace API_PRODUCAO.Models
{
    /// <summary>
    /// Audit trail de todos os movimentos de estoque de A Granel.
    /// </summary>
    public class MovimentoEstoqueAgranel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CodigoAgranel { get; set; } = string.Empty;

        /// <summary>ENTRADA_INICIAL | PREPARO | CONSUMO | AJUSTE | PERDA</summary>
        [Required]
        [MaxLength(30)]
        public string TipoMovimento { get; set; } = string.Empty;

        /// <summary>Quantidade em litros. Positivo para entrada, negativo para saída.</summary>
        public double QuantidadeLitros { get; set; }

        public double SaldoAnterior { get; set; }
        public double SaldoPosterior { get; set; }

        public DateTime DataMovimento { get; set; } = DateTime.UtcNow;

        /// <summary>OP #1234, Preparo #45, etc.</summary>
        [MaxLength(50)]
        public string? Referencia { get; set; }

        public string? Observacao { get; set; }

        [MaxLength(100)]
        public string? Usuario { get; set; }
    }
}
