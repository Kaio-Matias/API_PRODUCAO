using System.ComponentModel.DataAnnotations;

namespace API_PRODUCAO.Models
{
    public class DetalhamentoOP
    {
        [Key]
        public int Id { get; set; }

        public int OrdemProducao { get; set; }
        public string? Operador { get; set; }
        public string? Turno { get; set; }
        public int EmbProcessadas { get; set; }
        public int EmbProduzidas { get; set; }
        public int EmbPerdidas { get; set; }
    }
}
