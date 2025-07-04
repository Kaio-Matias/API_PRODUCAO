// Verifique se o namespace está EXATAMENTE assim.
using API_PRODUCAO.DTOs;

namespace API_PRODUCAO.Services.Interfaces
{
    public interface IPaletizacaoService
    {
        Task<Models.Paletizacao> CreatePaleteAsync(Models.Paletizacao palete);
        Task<IEnumerable<Models.Paletizacao>> GetPaletesByOpAsync(int ordemProducao);
        Task<PaleteDto> UpdateQtdePaleteAsync(int paleteId, UpdateQtdePaleteDto updateDto);
    }
}