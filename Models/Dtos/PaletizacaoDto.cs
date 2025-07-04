using System.ComponentModel.DataAnnotations;

namespace API_PRODUCAO.DTOs
{
    public class CreatePaleteDto
    {
        [Required]
        public int N_Palete { get; set; }
        [Required]
        public int OrdemProducao { get; set; }
        public string? CodigoProduto { get; set; }
        public string? Produto { get; set; }
        public string? Unidade { get; set; }
        public string? Maquina { get; set; }
        public string? Usuario { get; set; }
        [Required]
        public int QtdeCx { get; set; }
        [Required]
        public int QtdePorPalete { get; set; }
        [Required]
        public int QtdeProduzida { get; set; }
        public string? Bloqueio { get; set; }
    }

    public class PaleteDto
    {
        public int Id { get; set; }
        public int N_Palete { get; set; }
        public int OrdemProducao { get; set; }
        public string? CodigoProduto { get; set; }
        public string? Produto { get; set; }
        public int QtdePorPalete { get; set; }
        public string? Usuario { get; set; }
        public int QtdeCx { get; set; }
        public int QtdeProduzida { get; set; }
        public string? Bloqueio { get; set; }
        public DateTime DataHoraPaletizacao { get; set; }
    }
}