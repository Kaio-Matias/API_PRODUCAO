using System.ComponentModel.DataAnnotations;

namespace API_PRODUCAO.Models
{
    public class MetaCaptacaoLeite
    {
        [Key]
        public int Id { get; set; }
        public int Mes { get; set; }
        public int Ano { get; set; }
        public double MetaLitros { get; set; }
    }
}
