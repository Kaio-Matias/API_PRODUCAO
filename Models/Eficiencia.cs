using System.ComponentModel.DataAnnotations;
using System;

namespace API_PRODUCAO.Models
{
    public class Eficiencia
    {
        [Key]
        public int Id { get; set; }
        public int OrdemProducao { get; set; }
        public string? Motivo { get; set; }

        // Alterado de decimal para TimeSpan
        public TimeSpan Tempo { get; set; }
        public string? Operador { get; set; }
    }
}
