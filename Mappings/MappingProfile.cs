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

            // Mapeamentos para o Relatório Completo (Seus mapeamentos existentes - MANTER)
            CreateMap<Producoes, RelatorioOpCompletoDto>()
                .ForMember(dest => dest.InfoGeral, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Detalhamentos, opt => opt.MapFrom(src => src.DetalhamentoOPs))
                .ForMember(dest => dest.Perdas, opt => opt.MapFrom(src => src.Perdas))
                .ForMember(dest => dest.Paradas, opt => opt.MapFrom(src => src.Eficiencia))
                .ForMember(dest => dest.Paletes, opt => opt.MapFrom(src => src.Paletizacoes));

            CreateMap<DetalhamentoOP, DetalhamentoOpDto>();
            CreateMap<Perdas, PerdasOpDto>(); // Mapeamento existente para Relatórios
            CreateMap<Eficiencia, EficienciaOpDto>(); // Mapeamento existente para Relatórios
            CreateMap<Cadastro, CadastroDto>().ReverseMap();

            // =================================================================
            // ===== ADICIONE ESTAS DUAS LINHAS PARA CORRIGIR O ERRO 500 =====
            // =================================================================
            // Mapeamentos necessários para GetPerdasPorOpEOperador
            CreateMap<Perdas, PerdasDto>().ReverseMap();
            // Mapeamentos necessários para GetEficienciaPorOpEOperador
            CreateMap<Eficiencia, EficienciaDto>().ReverseMap();
        }
    }
}