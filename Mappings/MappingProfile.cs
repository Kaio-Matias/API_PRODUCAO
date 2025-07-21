using API_PRODUCAO.Models;
using AutoMapper;
using Valedourado.Shared.Dtos;

namespace API_PRODUCAO.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeamento para Usuário (O que faltava)
            CreateMap<Usuarios, UsuarioDto>();
            CreateMap<CreateUsuarioDto, Usuarios>();

            // Mapeamento para Produção
            CreateMap<Producoes, ProducaoDto>();
            CreateMap<CreateProducaoDto, Producoes>();

            // Mapeamento para Paletização
            CreateMap<CreatePaleteDto, Paletizacao>();
            CreateMap<Paletizacao, PaleteDto>();

            // Mapeamentos para o Relatório Completo
            CreateMap<Producoes, RelatorioOpCompletoDto>()
                .ForMember(dest => dest.InfoGeral, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Detalhamentos, opt => opt.MapFrom(src => src.DetalhamentoOPs))
                .ForMember(dest => dest.Perdas, opt => opt.MapFrom(src => src.Perdas))
                .ForMember(dest => dest.Paradas, opt => opt.MapFrom(src => src.Eficiencia))
                .ForMember(dest => dest.Paletes, opt => opt.MapFrom(src => src.Paletizacoes));

            CreateMap<DetalhamentoOP, DetalhamentoOpDto>();
            CreateMap<Perdas, PerdasOpDto>();
            CreateMap<Eficiencia, EficienciaOpDto>();
            CreateMap<Cadastro, CadastroDto>().ReverseMap();
        }
    }
}