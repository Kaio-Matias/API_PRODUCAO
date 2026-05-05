using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace API_PRODUCAO.Models
{
    public class Eficiencia
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Producao")]
        public int OrdemProducao { get; set; }

        public string? Motivo { get; set; }

        /// <summary>Momento exato em que a parada foi iniciada.</summary>
        public DateTime DataHoraInicio { get; set; }

        /// <summary>Momento exato em que a parada foi encerrada. Null = parada em andamento.</summary>
        public DateTime? DataHoraFim { get; set; }

        /// <summary>Duração armazenada (calculada ao finalizar). Mantida para compatibilidade.</summary>
        public TimeSpan? Tempo { get; set; }

        public string? Operador { get; set; }
        public DateTime DataRegistro { get; set; }

        public virtual Producoes? Producao { get; set; }

        // ── Campos computados (não mapeados) ──────────────────────
        [NotMapped]
        public bool EmAndamento => !DataHoraFim.HasValue;

        [NotMapped]
        public TimeSpan DuracaoAtual => DataHoraFim.HasValue
            ? DataHoraFim.Value - DataHoraInicio
            : DateTime.Now    - DataHoraInicio;
    }
}
