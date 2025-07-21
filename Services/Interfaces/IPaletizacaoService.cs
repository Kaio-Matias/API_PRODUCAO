// No arquivo /Services/Interfaces/IPaletizacaoService.cs
using Valedourado.Shared.Dtos;
using API_PRODUCAO.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPaletizacaoService
{
    // ALTERE AQUI: O método agora deve aceitar um CreatePaleteDto
    Task<Paletizacao> CreatePaleteAsync(CreatePaleteDto createPaleteDto);

    Task<IEnumerable<Paletizacao>> GetPaletesByOpAsync(int ordemProducao);
    Task<PaleteDto> UpdateQtdePaleteAsync(int paleteId, UpdateQtdePaleteDto updateDto);
}