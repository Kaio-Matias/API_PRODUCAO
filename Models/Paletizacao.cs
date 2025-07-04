using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API_PRODUCAO.Models
{
    public class Paletizacao
    {
        [Key]
        public int Id { get; set; }

        public int N_Palete { get; set; }
        public int OrdemProducao { get; set; }
        public string? CodigoProduto { get; set; }
        public string? Produto { get; set; }
        public string? Unidade { get; set; }
        public string? Maquina { get; set; }
        public string? Usuario { get; set; }
        public int QtdeCx { get; set; }
        public int QtdePorPalete { get; set; }
        public int QtdeProduzida { get; set; }

        // CORREÇÃO: Altere o tipo desta propriedade de 'bool' para 'string?'
        public string? Bloqueio { get; set; }

        public DateTime DataHoraPaletizacao { get; set; }
    }
}