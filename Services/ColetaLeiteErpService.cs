using System.Data;
using System.Text.RegularExpressions;
using API_PRODUCAO.Data;
using API_PRODUCAO.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace API_PRODUCAO.Services
{
    /// <summary>
    /// Configuração da leitura das coletas no banco do sistema do leite.
    /// Fica em appsettings → "LeiteErp" (ou variáveis de ambiente LeiteErp__Servidor, LeiteErp__Senha...).
    /// Basta preencher Servidor e Banco (+ Usuario/Senha); sem eles nada é executado.
    /// </summary>
    public class LeiteErpOptions
    {
        /// <summary>Servidor SQL do leite, ex.: "SERVIDOR\INSTANCIA" ou "192.168.0.10,1433".</summary>
        public string? Servidor { get; set; }

        /// <summary>Nome do banco do sistema do leite.</summary>
        public string? Banco { get; set; }

        /// <summary>Usuário SQL SOMENTE LEITURA. Vazio = autenticação do Windows do serviço da API.</summary>
        public string? Usuario { get; set; }

        public string? Senha { get; set; }

        /// <summary>Opcional (avançado): conexão completa. Se preenchida, substitui Servidor/Banco/Usuario/Senha.</summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Opcional: SELECT próprio. Vazio = consulta padrão (viagens L32 + litros por produtor L30).
        /// Deve devolver UMA linha por coleta com os aliases: NumeroColeta, DataHoraColeta, PlacaCaminhao,
        /// Transportador, Linha, VolumeDescarregado, VolumeDetalhado (Produtor é opcional). Recebe @inicio e @fim (datas).
        /// </summary>
        public string? ConsultaColetas { get; set; }

        /// <summary>Liga a sincronização automática em segundo plano.</summary>
        public bool SincronizacaoAutomatica { get; set; } = true;

        /// <summary>Intervalo da sincronização automática, em minutos (mínimo 5).</summary>
        public int IntervaloMinutos { get; set; } = 15;

        /// <summary>
        /// Quantos meses anteriores ao atual a sincronização automática também relê (0 a 12).
        /// Padrão 2 = últimos 3 meses (atual + 2), para pegar correções feitas depois no sistema do leite.
        /// </summary>
        public int MesesAnteriores { get; set; } = 2;
    }

    public record ColetaErp(
        string NumeroColeta, DateTime DataHoraColeta, string PlacaCaminhao, string Transportador,
        string Linha, string Produtor, double VolumeDescarregado, double VolumeDetalhado);

    public record ResultadoSincronizacao(
        DateTime Inicio, DateTime Fim, int LidasNoErp, int Inseridas, int Atualizadas, int SemMudanca,
        DateTime ExecutadaEm, string? Erro);

    /// <summary>
    /// Lê as coletas do banco do leite e espelha na tabela ColetasLeite do Portal
    /// (insere as novas e atualiza as existentes pelo Nº da coleta, que é único).
    /// </summary>
    public class ColetaLeiteErpService
    {
        private static readonly string[] ColunasObrigatorias =
        {
            "NumeroColeta", "DataHoraColeta", "PlacaCaminhao", "Transportador",
            "Linha", "VolumeDescarregado", "VolumeDetalhado",
        };

        // Última execução (manual ou automática), exibida na tela
        private static ResultadoSincronizacao? _ultima;
        private static readonly SemaphoreSlim _trava = new(1, 1);

        private readonly AppDbContext _db;
        private readonly LeiteErpOptions _opcoes;
        private readonly ILogger<ColetaLeiteErpService> _log;

        public ColetaLeiteErpService(AppDbContext db, IConfiguration config, ILogger<ColetaLeiteErpService> log)
        {
            _db = db;
            _log = log;
            _opcoes = config.GetSection("LeiteErp").Get<LeiteErpOptions>() ?? new LeiteErpOptions();
        }

        /// <summary>
        /// Consulta padrão do sistema do leite: uma linha por viagem (L32).
        /// Volume detalhado = soma dos litros por produtor (L30) da viagem.
        /// Volume descarregado = volume da viagem (L32_VOL; se vazio, L32_VOL_COLETADO).
        /// </summary>
        public const string ConsultaPadrao = @"
SELECT
    CAST(COALESCE(NULLIF(LTRIM(RTRIM(v.L32_CHAVE)), ''), 'V' + CAST(v.L32_ID AS varchar(20))) AS varchar(40)) AS NumeroColeta,
    COALESCE(CAST(v.L32_DATA AS datetime) + CAST(v.L32_HR_CHEGADA AS datetime), v.L32_HR_OPER_FIM, CAST(v.L32_DATA AS datetime)) AS DataHoraColeta,
    LTRIM(RTRIM(v.L32_PLACA)) AS PlacaCaminhao,
    COALESCE(NULLIF(LTRIM(RTRIM(t.A00_FANTASIA)), ''), LTRIM(RTRIM(t.A00_NOME)), '') AS Transportador,
    ISNULL(LTRIM(RTRIM(l.L04_DESC)), '') AS Linha,
    ISNULL(pr.Nomes, '') AS Produtor,
    COALESCE(NULLIF(v.L32_VOL, 0), v.L32_VOL_COLETADO, 0) AS VolumeDescarregado,
    COALESCE(p.Litros, v.L32_VOL_COLETADO, 0) AS VolumeDetalhado
FROM L32 v WITH (NOLOCK)
LEFT JOIN A00 t WITH (NOLOCK) ON t.A00_ID = v.L32_ID_TRANSP
LEFT JOIN L04 l WITH (NOLOCK) ON l.L04_ID = v.L32_ID_L04
OUTER APPLY (SELECT SUM(x.L30_LT) AS Litros
             FROM L30 x WITH (NOLOCK)
             WHERE x.L30_ID_L32 = v.L32_ID AND ISNULL(x.L30_DELETED, 0) = 0) p
OUTER APPLY (SELECT STUFF((
                 SELECT DISTINCT ', ' + COALESCE(NULLIF(LTRIM(RTRIM(pa.A00_FANTASIA)), ''), LTRIM(RTRIM(pa.A00_NOME)))
                 FROM L30 y WITH (NOLOCK)
                 JOIN A00 pa WITH (NOLOCK) ON pa.A00_ID = y.L30_ID_PROD
                 WHERE y.L30_ID_L32 = v.L32_ID AND ISNULL(y.L30_DELETED, 0) = 0
                 FOR XML PATH(''), TYPE).value('.', 'varchar(max)'), 1, 2, '') AS Nomes) pr
WHERE ISNULL(v.L32_DELETED, 0) = 0
  AND v.L32_DATA BETWEEN CAST(@inicio AS date) AND CAST(@fim AS date)";

        public bool Configurado =>
            !string.IsNullOrWhiteSpace(_opcoes.ConnectionString) ||
            (!string.IsNullOrWhiteSpace(_opcoes.Servidor) && !string.IsNullOrWhiteSpace(_opcoes.Banco));

        public LeiteErpOptions Opcoes => _opcoes;
        public static ResultadoSincronizacao? UltimaSincronizacao => _ultima;

        /// <summary>Servidor/banco em uso (sem senha), para exibir na tela.</summary>
        public string? Fonte
        {
            get
            {
                if (!Configurado) return null;
                try
                {
                    var b = new SqlConnectionStringBuilder(MontarConexao());
                    return $"{b.DataSource} / {b.InitialCatalog}";
                }
                catch (ArgumentException) { return "conexão inválida"; }
            }
        }

        /// <summary>Monta a conexão a partir dos campos (ou usa a ConnectionString completa).</summary>
        private string MontarConexao()
        {
            if (!string.IsNullOrWhiteSpace(_opcoes.ConnectionString)) return _opcoes.ConnectionString;

            var b = new SqlConnectionStringBuilder
            {
                DataSource = _opcoes.Servidor!.Trim(),
                InitialCatalog = _opcoes.Banco!.Trim(),
                TrustServerCertificate = true,
                ApplicationIntent = ApplicationIntent.ReadOnly,
                ApplicationName = "API_PRODUCAO - Coletas de leite",
                ConnectTimeout = 15,
            };
            if (string.IsNullOrWhiteSpace(_opcoes.Usuario))
            {
                b.IntegratedSecurity = true;
            }
            else
            {
                b.UserID = _opcoes.Usuario.Trim();
                b.Password = _opcoes.Senha ?? string.Empty;
            }
            return b.ConnectionString;
        }

        /// <summary>Lê as coletas do período no banco do leite (somente leitura).</summary>
        public async Task<List<ColetaErp>> BuscarAsync(DateTime inicio, DateTime fim, CancellationToken ct)
        {
            if (!Configurado) throw new InvalidOperationException("Integração com o banco do leite não configurada (LeiteErp: Servidor e Banco).");

            var sql = (string.IsNullOrWhiteSpace(_opcoes.ConsultaColetas) ? ConsultaPadrao : _opcoes.ConsultaColetas).Trim();
            // Proteção: a consulta configurada só pode LER dados
            if (!Regex.IsMatch(sql, @"^\s*(SELECT|WITH)\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(sql, @"\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|TRUNCATE|EXEC|EXECUTE|CREATE|GRANT)\b", RegexOptions.IgnoreCase) ||
                sql.TrimEnd(';').Contains(';'))
                throw new InvalidOperationException("LeiteErp:ConsultaColetas deve ser um único SELECT (somente leitura).");

            var resultado = new List<ColetaErp>();
            await using var conn = new SqlConnection(MontarConexao());
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            cmd.Parameters.Add("@inicio", SqlDbType.DateTime2).Value = inicio;
            cmd.Parameters.Add("@fim", SqlDbType.DateTime2).Value = fim;

            await using var rd = await cmd.ExecuteReaderAsync(ct);
            var cols = Enumerable.Range(0, rd.FieldCount).ToDictionary(i => rd.GetName(i), i => i, StringComparer.OrdinalIgnoreCase);
            var faltando = ColunasObrigatorias.Where(c => !cols.ContainsKey(c)).ToList();
            if (faltando.Count > 0)
                throw new InvalidOperationException($"A consulta do leite não devolve as colunas: {string.Join(", ", faltando)}.");

            object? Valor(string c) => rd.IsDBNull(cols[c]) ? null : rd.GetValue(cols[c]);

            while (await rd.ReadAsync(ct))
            {
                var numero = Convert.ToString(Valor("NumeroColeta"))?.Trim();
                var data = Valor("DataHoraColeta");
                if (string.IsNullOrEmpty(numero) || data is null) continue;

                resultado.Add(new ColetaErp(
                    Cortar(numero, 40),
                    Convert.ToDateTime(data),
                    Cortar(Convert.ToString(Valor("PlacaCaminhao"))?.Trim().ToUpperInvariant(), 10),
                    Cortar(Convert.ToString(Valor("Transportador"))?.Trim(), 150),
                    Cortar(Convert.ToString(Valor("Linha"))?.Trim(), 100),
                    // Produtor é opcional numa consulta própria
                    cols.ContainsKey("Produtor") ? Cortar(Convert.ToString(Valor("Produtor"))?.Trim(), 300) : string.Empty,
                    Convert.ToDouble(Valor("VolumeDescarregado") ?? 0),
                    Convert.ToDouble(Valor("VolumeDetalhado") ?? 0)));
            }

            // Se a consulta devolver o mesmo Nº mais de uma vez, mantém a última linha
            return resultado.GroupBy(c => c.NumeroColeta).Select(g => g.Last()).ToList();
        }

        /// <summary>Sincroniza o período: insere coletas novas e atualiza as que mudaram no sistema do leite.</summary>
        public async Task<ResultadoSincronizacao> SincronizarAsync(DateTime inicio, DateTime fim, CancellationToken ct)
        {
            await _trava.WaitAsync(ct); // evita duas sincronizações ao mesmo tempo
            try
            {
                List<ColetaErp> doErp;
                try
                {
                    doErp = await BuscarAsync(inicio, fim, ct);
                }
                catch (Exception ex) when (ex is SqlException or InvalidOperationException or InvalidCastException or FormatException)
                {
                    _log.LogError(ex, "Falha ao ler coletas do banco do leite");
                    return _ultima = new ResultadoSincronizacao(inicio, fim, 0, 0, 0, 0, DateTime.Now, ex.Message);
                }

                var numeros = doErp.Select(c => c.NumeroColeta).ToList();
                var locais = await _db.ColetasLeite
                    .Where(c => numeros.Contains(c.NumeroColeta))
                    .ToDictionaryAsync(c => c.NumeroColeta, ct);

                int ins = 0, upd = 0, igual = 0;
                foreach (var e in doErp)
                {
                    if (!locais.TryGetValue(e.NumeroColeta, out var l))
                    {
                        _db.ColetasLeite.Add(new ColetaLeite
                        {
                            NumeroColeta = e.NumeroColeta,
                            DataHoraColeta = e.DataHoraColeta,
                            PlacaCaminhao = e.PlacaCaminhao,
                            Transportador = e.Transportador,
                            Linha = e.Linha,
                            Produtor = e.Produtor,
                            VolumeDescarregado = e.VolumeDescarregado,
                            VolumeDetalhado = e.VolumeDetalhado,
                        });
                        ins++;
                        continue;
                    }

                    // O sistema do leite é a fonte oficial: o que vier de lá prevalece
                    var mudou = l.DataHoraColeta != e.DataHoraColeta || l.PlacaCaminhao != e.PlacaCaminhao ||
                                l.Transportador != e.Transportador || l.Linha != e.Linha || l.Produtor != e.Produtor ||
                                Math.Abs(l.VolumeDescarregado - e.VolumeDescarregado) > 0.0001 ||
                                Math.Abs(l.VolumeDetalhado - e.VolumeDetalhado) > 0.0001;
                    if (!mudou) { igual++; continue; }

                    l.DataHoraColeta = e.DataHoraColeta;
                    l.PlacaCaminhao = e.PlacaCaminhao;
                    l.Transportador = e.Transportador;
                    l.Linha = e.Linha;
                    l.Produtor = e.Produtor;
                    l.VolumeDescarregado = e.VolumeDescarregado;
                    l.VolumeDetalhado = e.VolumeDetalhado;
                    upd++;
                }

                await _db.SaveChangesAsync(ct);
                _log.LogInformation("Coletas de leite sincronizadas ({Inicio:d}–{Fim:d}): {Lidas} lidas, {Ins} novas, {Upd} atualizadas",
                    inicio, fim, doErp.Count, ins, upd);
                return _ultima = new ResultadoSincronizacao(inicio, fim, doErp.Count, ins, upd, igual, DateTime.Now, null);
            }
            finally
            {
                _trava.Release();
            }
        }

        /// <summary>
        /// Período padrão: do 1º dia de (mês atual − mesesAnteriores) até o fim do mês atual.
        /// Com 0, nos 3 primeiros dias ainda inclui o mês anterior (fechamentos atrasados).
        /// </summary>
        public static (DateTime Inicio, DateTime Fim) PeriodoPadrao(DateTime agora, int mesesAnteriores)
        {
            var inicioMes = new DateTime(agora.Year, agora.Month, 1);
            var meses = Math.Clamp(mesesAnteriores, 0, 12);
            if (meses == 0 && agora.Day <= 3) meses = 1;
            return (inicioMes.AddMonths(-meses), inicioMes.AddMonths(1).AddTicks(-1));
        }

        private static string Cortar(string? s, int max) =>
            string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);
    }

    /// <summary>Roda a sincronização das coletas de leite a cada N minutos, se estiver configurada.</summary>
    public class ColetaLeiteSyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<ColetaLeiteSyncWorker> _log;

        public ColetaLeiteSyncWorker(IServiceScopeFactory scopes, ILogger<ColetaLeiteSyncWorker> log)
        {
            _scopes = scopes;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Pequena espera para a API terminar de subir
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var intervalo = TimeSpan.FromMinutes(15);
                    try
                    {
                        using var scope = _scopes.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<ColetaLeiteErpService>();
                        intervalo = TimeSpan.FromMinutes(Math.Max(5, svc.Opcoes.IntervaloMinutos));

                        if (svc.Configurado && svc.Opcoes.SincronizacaoAutomatica)
                        {
                            var (inicio, fim) = ColetaLeiteErpService.PeriodoPadrao(DateTime.Now, svc.Opcoes.MesesAnteriores);
                            await svc.SincronizarAsync(inicio, fim, stoppingToken);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Nunca derruba a API por falha na integração
                        _log.LogError(ex, "Erro na sincronização automática das coletas de leite");
                    }

                    await Task.Delay(intervalo, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // API sendo desligada
            }
        }
    }
}
