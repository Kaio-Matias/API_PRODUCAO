/* =====================================================================
   Descoberta do banco do sistema do leite  —  SOMENTE LEITURA
   ---------------------------------------------------------------------
   Objetivo: entender as tabelas de recepção/coleta (L32, L36, L29...)
   para montar a consulta LeiteErp:ConsultaColetas da API.

   Como rodar (SSMS, na área remota):
     1. Conecte no banco do leite e selecione o banco certo (USE ...).
     2. Execute tudo (F5). Cada parte aparece numa grade de resultado.
     3. Em cada grade: botão direito → "Copy with Headers" e cole no chat.

   Nada aqui altera dados: são apenas SELECTs em catálogo e TOP 5 linhas.
   Leitura sem bloqueio, para não atrapalhar o sistema em uso.
   ===================================================================== */

SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
GO

/* ---------------------------------------------------------------------
   PARTE 1 — Tabelas do leite: esquema, nº de linhas e colunas (1 linha por tabela)
   --------------------------------------------------------------------- */
SELECT
    SCHEMA_NAME(t.schema_id)                       AS Esquema,
    t.name                                         AS Tabela,
    ISNULL(p.Linhas, 0)                            AS Linhas,
    STUFF((
        SELECT ', ' + c.name + ' ' + ty.name +
               CASE WHEN ty.name IN ('varchar','char','varbinary') THEN '(' + CASE WHEN c.max_length = -1 THEN 'max' ELSE CAST(c.max_length AS varchar(10)) END + ')'
                    WHEN ty.name IN ('nvarchar','nchar')          THEN '(' + CASE WHEN c.max_length = -1 THEN 'max' ELSE CAST(c.max_length / 2 AS varchar(10)) END + ')'
                    WHEN ty.name IN ('decimal','numeric')         THEN '(' + CAST(c.precision AS varchar(5)) + ',' + CAST(c.scale AS varchar(5)) + ')'
                    ELSE '' END
        FROM sys.columns c
        JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE c.object_id = t.object_id
        ORDER BY c.column_id
        FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS Colunas
FROM sys.tables t
LEFT JOIN (
    SELECT object_id, SUM(rows) AS Linhas
    FROM sys.partitions
    WHERE index_id IN (0, 1)
    GROUP BY object_id
) p ON p.object_id = t.object_id
WHERE t.name IN ('L00','L04','L29','L30','L32','L33','L36','A00','A12','A29')
ORDER BY t.name;
GO

/* ---------------------------------------------------------------------
   PARTE 2 — Chaves primárias dessas tabelas
   --------------------------------------------------------------------- */
SELECT
    t.name   AS Tabela,
    STUFF((
        SELECT ', ' + c.name
        FROM sys.index_columns ic
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
        ORDER BY ic.key_ordinal
        FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS ChavePrimaria
FROM sys.tables t
JOIN sys.indexes i ON i.object_id = t.object_id AND i.is_primary_key = 1
WHERE t.name IN ('L00','L04','L29','L30','L32','L33','L36','A00','A12','A29')
ORDER BY t.name;
GO

/* ---------------------------------------------------------------------
   PARTE 3 — Relacionamentos (chaves estrangeiras) envolvendo essas tabelas
   (sistemas antigos às vezes não têm nenhuma; vazio é normal)
   --------------------------------------------------------------------- */
SELECT
    OBJECT_NAME(fk.parent_object_id)                                  AS Tabela,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id)              AS Coluna,
    OBJECT_NAME(fk.referenced_object_id)                              AS TabelaReferenciada,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id)      AS ColunaReferenciada
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
WHERE OBJECT_NAME(fk.parent_object_id)     IN ('L00','L04','L29','L30','L32','L33','L36','A00','A12','A29')
   OR OBJECT_NAME(fk.referenced_object_id) IN ('L00','L04','L29','L30','L32','L33','L36','A00','A12','A29')
ORDER BY Tabela, Coluna;
GO

/* ---------------------------------------------------------------------
   PARTE 4 — Colunas com nomes típicos de coleta em QUALQUER tabela
   (ajuda caso viagem/placa/volume estejam em outra tabela)
   --------------------------------------------------------------------- */
SELECT TOP 300
    t.name  AS Tabela,
    c.name  AS Coluna,
    ty.name AS Tipo
FROM sys.columns c
JOIN sys.tables t ON t.object_id = c.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE c.name LIKE '%placa%'  OR c.name LIKE '%viag%'   OR c.name LIKE '%colet%'
   OR c.name LIKE '%volum%'  OR c.name LIKE '%litr%'   OR c.name LIKE '%descarg%'
   OR c.name LIKE '%recep%'  OR c.name LIKE '%linha%'  OR c.name LIKE '%rota%'
   OR c.name LIKE '%transp%' OR c.name LIKE '%veic%'   OR c.name LIKE '%caminh%'
ORDER BY t.name, c.column_id;
GO

/* ---------------------------------------------------------------------
   PARTE 5 — 5 linhas mais recentes de cada tabela principal
   Cada bloco é separado por GO: se uma tabela não existir, as outras rodam.
   Se alguma coluna tiver dado pessoal (CPF, telefone...), apague antes de colar.
   --------------------------------------------------------------------- */
SELECT TOP 5 'L32' AS _tabela, * FROM L32 ORDER BY 2 DESC;
GO
SELECT TOP 5 'L36' AS _tabela, * FROM L36 ORDER BY 2 DESC;
GO
SELECT TOP 5 'L29' AS _tabela, * FROM L29 ORDER BY 2 DESC;
GO
SELECT TOP 5 'L30' AS _tabela, * FROM L30 ORDER BY 2 DESC;
GO
SELECT TOP 5 'L33' AS _tabela, * FROM L33 ORDER BY 2 DESC;
GO
SELECT TOP 5 'L04' AS _tabela, * FROM L04 ORDER BY 2 DESC;
GO
SELECT TOP 5 'A12' AS _tabela, * FROM A12 ORDER BY 2 DESC;
GO

/* ---------------------------------------------------------------------
   PARTE 6 — Versão do SQL Server (define que funções posso usar na consulta final)
   --------------------------------------------------------------------- */
SELECT @@VERSION AS VersaoSqlServer, DB_NAME() AS BancoAtual;
GO
