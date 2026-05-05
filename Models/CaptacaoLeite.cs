using System.ComponentModel.DataAnnotations;

namespace API_PRODUCAO.Models
{
    public class CaptacaoLeite
    {
        [Key]
        public int Id { get; set; }
        public DateTime DataRecebimento { get; set; }
        public double VolumeRecebido { get; set; }
        public string? Observacao { get; set; }
    }
}
