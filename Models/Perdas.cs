using System.ComponentModel.DataAnnotations;

namespace API_PRODUCAO.Models
{
    public class Perdas
    {
        [Key]
        public int Id { get; set; }
        public int OrdemProducao { get; set; }
        public string? Motivo { get; set; }
        public int Quantidade { get; set; }
        public string? Operador { get; set; }
    }
}
