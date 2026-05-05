using API_PRODUCAO.Models;
using AutoMapper;
using Valedourado.Shared.Dtos;

namespace API_PRODUCAO.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Usuarios, UsuarioDto>();
            CreateMap<CreateUsuarioDto, Usuarios>();

            CreateMap<Producoes, ProducaoDto>();
            CreateMap<ProducaoDto, Producoes>()
                .ForMember(d => d.OrdemProducao,     o => o.MapFrom(s => s.OrdemProducao))
                .ForMember(d => d.DetalhamentoOPs,   o => o.Ignore())
                .ForMember(d => d.Perdas,            o => o.Ignore())
                .ForMember(d => d.Eficiencia,        o => o.Ignore())
                .ForMember(d => d.Paletizacoes,      o => o.Ignore());
            CreateMap<CreateProducaoDto, Producoes>();

            CreateMap<CreatePaleteDto, Paletizacao>();
            CreateMap<Paletizacao, PaleteDto>();

            CreateMap<DetalhamentoOP, DetalhamentoOpDto>();
            CreateMap<Perdas, PerdasOpDto>();
            CreateMap<Perdas, PerdasDto>().ReverseMap();
            CreateMap<Cadastro, CadastroDto>().ReverseMap();

            // Eficiência → EficienciaDto (com campos de timestamp)
            CreateMap<Eficiencia, EficienciaDto>()
                .ForMember(d => d.EmAndamento, o => o.MapFrom(s => !s.DataHoraFim.HasValue))
                .ForMember(d => d.Tempo, o => o.MapFrom(s =>
                    s.Tempo ?? (s.DataHoraFim.HasValue ? s.DataHoraFim.Value - s.DataHoraInicio : (TimeSpan?)null)));

            // Eficiência → EficienciaOpDto (relatório de OP)
            CreateMap<Eficiencia, EficienciaOpDto>()
                .ForMember(d => d.EmAndamento, o => o.MapFrom(s => !s.DataHoraFim.HasValue))
                .ForMember(d => d.Tempo, o => o.MapFrom(s =>
                    s.Tempo.HasValue
                        ? s.Tempo.Value.ToString(@"hh\:mm\:ss")
                        : (s.DataHoraFim.HasValue
                            ? (s.DataHoraFim.Value - s.DataHoraInicio).ToString(@"hh\:mm\:ss")
                            : (string?)null)));

            // Producoes → RelatorioOpCompletoDto
            CreateMap<Producoes, RelatorioOpCompletoDto>()
                .ForMember(d => d.InfoGeral, o => o.MapFrom(s => s))
                .ForMember(d => d.Detalhamentos, o => o.MapFrom(s => s.DetalhamentoOPs))
                .ForMember(d => d.Perdas, o => o.MapFrom(s => s.Perdas))
                .ForMember(d => d.Paradas, o => o.MapFrom(s => s.Eficiencia))
                .ForMember(d => d.Paletes, o => o.MapFrom(s => s.Paletizacoes));
        }
    }
}
