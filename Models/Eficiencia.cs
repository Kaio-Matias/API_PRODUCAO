using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // ADICIONE ESTE USING
using System;

namespace API_PRODUCAO.Models
{
    public class Eficiencia
    {
        [Key]
        public int Id { get; set; }

        // ADICIONE O ATRIBUTO [ForeignKey] AQUI
        [ForeignKey("Producao")]
        public int OrdemProducao { get; set; }

        public string? Motivo { get; set; }
        public TimeSpan Tempo { get; set; }
        public string? Operador { get; set; }
        public DateTime DataRegistro { get; set; }
        public virtual Producoes? Producao { get; set; }
    }
}