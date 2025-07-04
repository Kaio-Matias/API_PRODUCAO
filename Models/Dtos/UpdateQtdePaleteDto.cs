namespace API_PRODUCAO.DTOs
{
    // DTO específico para a atualização, enviando apenas o dado que muda.
    public class UpdateQtdePaleteDto
    {
        public int NovaQtdePorPalete { get; set; }
    }
}