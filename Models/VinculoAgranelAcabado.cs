using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_PRODUCAO.Models
{
    public class VinculoAgranelAcabado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CodigoAgranel { get; set; } = string.Empty;

        public string DescricaoAgranel { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CodigoProdutoAcabado { get; set; } = string.Empty;

        public string DescricaoProdutoAcabado { get; set; } = string.Empty;

        // Fator de conversão (Litros por caixa). Por exemplo, 1 caixa de 27x200ml = 5.4 Litros.
        public double LitrosPorCaixa { get; set; }
    }
}
