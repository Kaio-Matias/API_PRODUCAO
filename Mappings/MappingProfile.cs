// ARQUIVO: /Mappings/MappingProfile.cs (na API)

using AutoMapper;
using API_PRODUCAO.Models;
using API_PRODUCAO.DTOs;

namespace API_PRODUCAO.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeamento para criação (DTO -> Entidade)
            CreateMap<CreatePaleteDto, Paletizacao>();
            CreateMap<CreateProducaoDto, Producoes>();
            CreateMap<CreateUsuarioDto, Usuarios>();

            // Mapeamento para leitura (Entidade -> DTO)
            CreateMap<Cadastro, CadastroDto>();
            CreateMap<Producoes, ProducaoDto>();
            CreateMap<Usuarios, UsuarioDto>();

            // CORREÇÃO ESTÁ AQUI:
            // O mapeamento de Paletizacao para PaleteDto agora é direto,
            // pois assumimos que ambos usam 'string' para a propriedade 'Bloqueio'.
            // A regra customizada foi removida para evitar o FormatException.
            CreateMap<Paletizacao, PaleteDto>();
        }
    }
}