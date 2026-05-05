using System.ComponentModel.DataAnnotations;

namespace API_PRODUCAO.Models
{
    public class Producoes
    {
        [Key]
        public int OrdemProducao { get; set; }
        public string? Produto { get; set; }
        public string? Maquina { get; set; }
        public string? Unidade { get; set; }
        public string? Status { get; set; }
        public DateTime DataHoraAbertura { get; set; }
        public DateTime? DataHoraFechamento { get; set; }
        public string? SupervisorFechamento { get; set; }

        public string? CodigoAgranel { get; set; }
        public double FatorConversaoLiters { get; set; }


        public virtual ICollection<DetalhamentoOP> DetalhamentoOPs { get; set; }
        public virtual ICollection<Perdas> Perdas { get; set; }
        public virtual ICollection<Eficiencia> Eficiencia { get; set; }
        public virtual ICollection<Paletizacao> Paletizacoes { get; set; }
    }
}