using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Adicione este using

namespace API_PRODUCAO.Models
{
    public class Perdas
    {
        [Key]
        public int Id { get; set; }

        // Chave estrangeira
        [ForeignKey("Producao")]
        public int OrdemProducao { get; set; }

        public string? Motivo { get; set; }
        public int Quantidade { get; set; }
        public string? Operador { get; set; }

        public DateTime DataRegistro { get; set; }
        public virtual Producoes? Producao { get; set; }
    }
}