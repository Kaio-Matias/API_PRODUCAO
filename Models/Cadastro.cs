namespace API_PRODUCAO.Models
{
    public class Cadastro
    {
        public int Id { get; set; }
        public string? CodProduto { get; set; }
        public string? Produto { get; set; }
        public string? CodBarra { get; set; }
        public string? Maquina { get; set; }
        public string? Unidade { get; set; }
        public string? Classe { get; set; }
        public int QtdeCaixa { get; set; }
        public int QtdePorPalete { get; set; }
        public double PesoBruto { get; set; }
        public double PesoLiquido { get; set; }
        public double PesoTotalPalete { get; set; }
        public decimal PrecoUnitario { get; set; }
        public bool Terceirizado { get; set; }
    }
}
