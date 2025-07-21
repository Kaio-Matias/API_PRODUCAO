using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Adicione este using

namespace API_PRODUCAO.Models
{
    public class DetalhamentoOP
    {
        [Key]
        public int Id { get; set; }

        // Esta é a chave estrangeira que aponta para a tabela Producoes
        [ForeignKey("Producao")]
        public int OrdemProducao { get; set; }

        public string? Operador { get; set; }
        public string? Turno { get; set; }
        public int EmbProcessadas { get; set; }
        public int EmbProduzidas { get; set; }
        public int EmbPerdidas { get; set; }

        // Esta é a propriedade de navegação
        public virtual Producoes? Producao { get; set; }
    }
}