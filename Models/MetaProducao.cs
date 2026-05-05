using System.ComponentModel.DataAnnotations;

namespace API_PRODUCAO.Models
{
    public class MetaProducao
    {
        [Key]
        public int Id { get; set; }
        public int Mes { get; set; }
        public int Ano { get; set; }
        public string? Classe { get; set; }
        /// <summary>
        /// Quando preenchido, a meta é por produto específico.
        /// Quando nulo, a meta é por classe.
        /// </summary>
        public string? CodProduto { get; set; }
        public double MetaCaixas { get; set; }
        public double MetaKg { get; set; }
        public double MetaRS { get; set; }
    }
}
