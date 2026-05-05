using Microsoft.EntityFrameworkCore;
using API_PRODUCAO.Models;

namespace API_PRODUCAO.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Cadastro> Cadastros { get; set; }
        public DbSet<Producoes> Producoes { get; set; }
        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<DetalhamentoOP> DetalhamentoOPs { get; set; }
        public DbSet<Perdas> Perdas { get; set; }
        public DbSet<Eficiencia> Eficiencia { get; set; }
        public DbSet<Paletizacao> Paletizacoes { get; set; }
        public DbSet<MetaProducao> MetasProducao { get; set; }
        public DbSet<MetaCaptacaoLeite> MetasCaptacaoLeite { get; set; }
        public DbSet<CaptacaoLeite> CaptacoesLeite { get; set; }
        public DbSet<VinculoAgranelAcabado> VinculosAgranelAcabado { get; set; }
        public DbSet<IndicadorAgranel> IndicadoresAgranel { get; set; }
        public DbSet<EstoqueAgranel> EstoqueAgranel { get; set; }
        public DbSet<MovimentoEstoqueAgranel> MovimentosEstoqueAgranel { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
