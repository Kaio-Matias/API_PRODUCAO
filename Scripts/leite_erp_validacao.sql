/* =====================================================================
   Banco do leite — RODADA 2: confirmação do mapeamento  (SOMENTE LEITURA)
   ---------------------------------------------------------------------
   Rode tudo (F5) no banco do leite e cole cada grade no chat
   (botão direito → "Copy with Headers").
   ===================================================================== */

SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
GO

/* ---------------------------------------------------------------------
   PARTE A — 15 viagens MAIS RECENTES (L32) já com transportador, linha
   e as somas por produtor (L30) e por compartimento (L33)
   --------------------------------------------------------------------- */
SELECT TOP 15
    v.L32_ID, v.L32_CHAVE, v.L32_DATA, v.L32_STATUS, v.L32_RECEPCAO_MODO, v.L32_DS_COD,
    v.L32_PLACA, v.L32_ID_TRANSP,
    LTRIM(RTRIM(t.A00_NOME))      AS Transp_Nome,
    LTRIM(RTRIM(t.A00_FANTASIA))  AS Transp_Fantasia,
    v.L32_ID_L04, LTRIM(RTRIM(l.L04_DESC)) AS Linha,
    v.L32_HR_CHEGADA, v.L32_HR_OPER_INI, v.L32_HR_OPER_FIM, v.L32_HR_DESC_INI, v.L32_HR_DESC_FIM,
    v.L32_VOL_COLETADO, v.L32_VOL,
    p.Litros_L30, p.Produtores_L30, c.Litros_L33,
    v.L32_DELETED
FROM L32 v
LEFT JOIN A00 t ON t.A00_ID = v.L32_ID_TRANSP
LEFT JOIN L04 l ON l.L04_ID = v.L32_ID_L04
OUTER APPLY (SELECT SUM(x.L30_LT) AS Litros_L30, COUNT(*) AS Produtores_L30
             FROM L30 x WHERE x.L30_ID_L32 = v.L32_ID AND ISNULL(x.L30_DELETED, 0) = 0) p
OUTER APPLY (SELECT SUM(y.L33_VOLUME) AS Litros_L33
             FROM L33 y WHERE y.L33_ID_L32 = v.L32_ID AND ISNULL(y.L33_DELETED, 0) = 0) c
ORDER BY v.L32_ID DESC;
GO

/* ---------------------------------------------------------------------
   PARTE B — Totais por mês (últimos 6 meses): viagens e litros
   Compare com o total de leite recebido que vocês conhecem do mês.
   --------------------------------------------------------------------- */
SELECT
    CONVERT(char(7), v.L32_DATA, 120)                          AS Mes,
    COUNT(*)                                                   AS Viagens,
    SUM(CASE WHEN ISNULL(v.L32_DELETED, 0) = 1 THEN 1 ELSE 0 END) AS Excluidas,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(v.L32_CHAVE)), '') IS NULL THEN 1 ELSE 0 END) AS SemChave,
    SUM(ISNULL(v.L32_VOL_COLETADO, 0))                         AS Soma_VOL_COLETADO,
    SUM(ISNULL(v.L32_VOL, 0))                                  AS Soma_VOL,
    SUM(ISNULL(p.Litros, 0))                                   AS Soma_L30_LigadoViagem
FROM L32 v
OUTER APPLY (SELECT SUM(x.L30_LT) AS Litros
             FROM L30 x WHERE x.L30_ID_L32 = v.L32_ID AND ISNULL(x.L30_DELETED, 0) = 0) p
WHERE v.L32_DATA >= DATEADD(MONTH, -6, GETDATE())
GROUP BY CONVERT(char(7), v.L32_DATA, 120)
ORDER BY Mes DESC;
GO

/* ---------------------------------------------------------------------
   PARTE C — Litros por produtor (L30) por mês: quanto está ligado a uma viagem
   --------------------------------------------------------------------- */
SELECT
    CONVERT(char(7), L30_DATA, 120)                                   AS Mes,
    COUNT(*)                                                          AS Lancamentos,
    SUM(L30_LT)                                                       AS Litros_Total,
    SUM(CASE WHEN L30_ID_L32 IS NULL THEN L30_LT ELSE 0 END)          AS Litros_SemViagem,
    SUM(CASE WHEN L30_ID_L32 IS NULL THEN 1 ELSE 0 END)               AS Lanc_SemViagem
FROM L30
WHERE L30_DATA >= DATEADD(MONTH, -6, GETDATE()) AND ISNULL(L30_DELETED, 0) = 0
GROUP BY CONVERT(char(7), L30_DATA, 120)
ORDER BY Mes DESC;
GO

/* ---------------------------------------------------------------------
   PARTE D — Significado dos códigos (últimos 3 meses)
   --------------------------------------------------------------------- */
SELECT v.L32_STATUS, v.L32_RECEPCAO_MODO, v.L32_DS_COD,
       COUNT(*) AS Viagens,
       SUM(ISNULL(v.L32_VOL_COLETADO, 0)) AS Soma_VOL_COLETADO,
       SUM(ISNULL(v.L32_VOL, 0))          AS Soma_VOL
FROM L32 v
WHERE v.L32_DATA >= DATEADD(MONTH, -3, GETDATE())
GROUP BY v.L32_STATUS, v.L32_RECEPCAO_MODO, v.L32_DS_COD
ORDER BY Viagens DESC;
GO

/* ---------------------------------------------------------------------
   PARTE E — A L32_CHAVE é única? (será o Nº da coleta no Portal)
   --------------------------------------------------------------------- */
SELECT COUNT(*) AS Viagens,
       COUNT(DISTINCT NULLIF(LTRIM(RTRIM(L32_CHAVE)), '')) AS ChavesDistintas
FROM L32
WHERE L32_DATA >= DATEADD(MONTH, -12, GETDATE());

SELECT TOP 10 LTRIM(RTRIM(L32_CHAVE)) AS Chave, COUNT(*) AS Repeticoes
FROM L32
WHERE NULLIF(LTRIM(RTRIM(L32_CHAVE)), '') IS NOT NULL
GROUP BY LTRIM(RTRIM(L32_CHAVE))
HAVING COUNT(*) > 1
ORDER BY Repeticoes DESC;
GO

/* ---------------------------------------------------------------------
   PARTE F — Tabelas L27 e L45 (têm Nº coleta / placa / volume coletado)
   --------------------------------------------------------------------- */
SELECT t.name AS Tabela, ISNULL(p.Linhas, 0) AS Linhas,
       STUFF((SELECT ', ' + c.name + ' ' + ty.name
              FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id
              WHERE c.object_id = t.object_id ORDER BY c.column_id
              FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS Colunas
FROM sys.tables t
LEFT JOIN (SELECT object_id, SUM(rows) AS Linhas FROM sys.partitions WHERE index_id IN (0, 1) GROUP BY object_id) p
       ON p.object_id = t.object_id
WHERE t.name IN ('L27', 'L45');
GO
SELECT TOP 5 * FROM L27 ORDER BY 1 DESC;
GO
SELECT TOP 5 * FROM L45 ORDER BY 1 DESC;
GO

/* ---------------------------------------------------------------------
   PARTE G — PRÉVIA da consulta proposta para a sincronização (mês atual)
   É exatamente o formato que a API vai ler.
   --------------------------------------------------------------------- */
DECLARE @inicio datetime2 = DATEADD(DAY, 1, EOMONTH(GETDATE(), -1));
DECLARE @fim    datetime2 = DATEADD(DAY, 1, EOMONTH(GETDATE()));

SELECT TOP 30
    CAST(COALESCE(NULLIF(LTRIM(RTRIM(v.L32_CHAVE)), ''), 'V' + CAST(v.L32_ID AS varchar(20))) AS varchar(40)) AS NumeroColeta,
    COALESCE(CAST(v.L32_DATA AS datetime) + CAST(v.L32_HR_CHEGADA AS datetime), v.L32_HR_OPER_FIM, CAST(v.L32_DATA AS datetime)) AS DataHoraColeta,
    LTRIM(RTRIM(v.L32_PLACA))                                                          AS PlacaCaminhao,
    COALESCE(NULLIF(LTRIM(RTRIM(t.A00_FANTASIA)), ''), LTRIM(RTRIM(t.A00_NOME)), '')    AS Transportador,
    ISNULL(LTRIM(RTRIM(l.L04_DESC)), '')                                               AS Linha,
    ISNULL(v.L32_VOL, 0)                                                               AS VolumeDescarregado,
    COALESCE(p.Litros, v.L32_VOL_COLETADO, 0)                                          AS VolumeDetalhado
FROM L32 v
LEFT JOIN A00 t ON t.A00_ID = v.L32_ID_TRANSP
LEFT JOIN L04 l ON l.L04_ID = v.L32_ID_L04
OUTER APPLY (SELECT SUM(x.L30_LT) AS Litros
             FROM L30 x WHERE x.L30_ID_L32 = v.L32_ID AND ISNULL(x.L30_DELETED, 0) = 0) p
WHERE ISNULL(v.L32_DELETED, 0) = 0
  AND v.L32_DATA >= @inicio AND v.L32_DATA < @fim
ORDER BY v.L32_ID DESC;
GO

SELECT @@VERSION AS VersaoSqlServer, DB_NAME() AS BancoAtual;
GO
