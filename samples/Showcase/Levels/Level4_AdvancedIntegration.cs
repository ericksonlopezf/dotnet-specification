// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification.MariaDb;
using EricksonLopez.Specification.MsSql;
using EricksonLopez.Specification.MySql;
using EricksonLopez.Specification.Oracle;
using EricksonLopez.Specification.PostgreSql;
using EricksonLopez.Specification.Showcase.Domain;
using EricksonLopez.Specification.Sql;
using EricksonLopez.Specification.Sqlite;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Specification.Showcase.Levels;

/// <summary>
/// Level 4 — Advanced Integration.
/// Demonstrates the complete SQL layer:
/// <see cref="QuerySpecTranslator{T}"/>, <see cref="ISqlDialect"/>,
/// native database dialects (PostgreSQL, SQL Server, SQLite, MySQL, MariaDB, Oracle),
/// <see cref="IColumnNameResolver"/>, and <see cref="QueryModel"/> AST node types.
/// </summary>
public sealed class Level4_AdvancedIntegration : ILevel
{
    private readonly ILogger<Level4_AdvancedIntegration> _logger;

    /// <inheritdoc/>
    public string Name => "Level 4 — Advanced Integration";

    /// <inheritdoc/>
    public string Description => "Specification-to-SQL translation via QuerySpecTranslator + ISqlDialect across native database dialects.";

    /// <summary>
    /// Initializes a new instance of the level.
    /// </summary>
    /// <param name="logger">The logger used for output.</param>
    public Level4_AdvancedIntegration(ILogger<Level4_AdvancedIntegration> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync()
    {
        _logger.LogInformation("--- {Name} ---", Name);

        // ─────────────────────────────────────────────────────────────────
        // 1. Build a rich QuerySpec<T> with filters, ordering, and pagination
        // ─────────────────────────────────────────────────────────────────
        var activeSpec = new ActiveCustomerSpecification();
        var vipSpec = new VipCustomerSpecification(minimumPurchases: 10);

        var spec = QuerySpec<Customer>.Empty
            .And(activeSpec)
            .And(vipSpec)
            .OrderByDescending(c => c.TotalPurchases)
            .ThenBy(c => c.Name)
            .Page(page: 1, pageSize: 20);

        // ─────────────────────────────────────────────────────────────────
        // 2. QuerySpecTranslator<T> — converts expression tree into database-agnostic QueryModel
        //    • tableName: SQL table name.
        //    • columnNameResolver: name resolution strategy.
        //      SnakeCaseColumnNameResolver.Default (default):
        //         IsActive → is_active
        //         TotalPurchases → total_purchases
        // ─────────────────────────────────────────────────────────────────
        var translator = new QuerySpecTranslator<Customer>(
            tableName: "customers",
            columnNameResolver: SnakeCaseColumnNameResolver.Default);

        var queryModel = translator.Translate(spec);

        _logger.LogInformation("[QueryModel] Table: {T}  Filters: {F}  Orders: {O}  Skip: {S}  Take: {K}",
            queryModel.TableName,
            queryModel.Filters.Length,
            queryModel.Orders.Length,
            queryModel.Skip,
            queryModel.Take);

        // ─────────────────────────────────────────────────────────────────
        // 3. ISqlDialect — renders QueryModel into engine-specific SQL.
        //    PostgreSQL: double-quoted identifiers, @ parameters, LIMIT/OFFSET
        // ─────────────────────────────────────────────────────────────────
        var pgDialect = PostgreSqlDialect.Default;
        var pgQuery = pgDialect.Render(queryModel);

        _logger.LogInformation("[PostgreSQL] Dialect: {D}", pgDialect.DialectName);
        _logger.LogInformation("[PostgreSQL] SQL:\n{Sql}", pgQuery.Sql);
        _logger.LogInformation("[PostgreSQL] Params ({N}):", pgQuery.Parameters.Count);
        foreach (var p in pgQuery.Parameters)
            _logger.LogInformation("   @{K} = {V}", p.Key, p.Value);

        // ─────────────────────────────────────────────────────────────────
        // 4. MsSqlDialect — SQL Server: bracketed identifiers [], TOP N, OFFSET/FETCH
        // ─────────────────────────────────────────────────────────────────
        var mssqlDialect = MsSqlDialect.Default;
        var mssqlQuery = mssqlDialect.Render(queryModel);

        _logger.LogInformation("[SQL Server] Dialect: {D}", mssqlDialect.DialectName);
        _logger.LogInformation("[SQL Server] SQL:\n{Sql}", mssqlQuery.Sql);

        // ─────────────────────────────────────────────────────────────────
        // 5. SqliteDialect — SQLite: double-quoted identifiers, @ parameters, LIMIT/OFFSET
        // ─────────────────────────────────────────────────────────────────
        var sqliteDialect = SqliteDialect.Default;
        var sqliteQuery = sqliteDialect.Render(queryModel);

        _logger.LogInformation("[SQLite] Dialect: {D}", sqliteDialect.DialectName);
        _logger.LogInformation("[SQLite] SQL:\n{Sql}", sqliteQuery.Sql);

        // ─────────────────────────────────────────────────────────────────
        // 6. MySqlDialect & MariaDbDialect — MySQL & MariaDB: backticks, @params, LIMIT/OFFSET
        // ─────────────────────────────────────────────────────────────────
        var mysqlDialect = MySqlDialect.Default;
        var mysqlQuery = mysqlDialect.Render(queryModel);
        var mariadbDialect = MariaDbDialect.Default;
        var mariadbQuery = mariadbDialect.Render(queryModel);

        _logger.LogInformation("[MySQL] Dialect: {D}", mysqlDialect.DialectName);
        _logger.LogInformation("[MySQL] SQL:\n{Sql}", mysqlQuery.Sql);
        _logger.LogInformation("[MariaDB] Dialect: {D}", mariadbDialect.DialectName);
        _logger.LogInformation("[MariaDB] SQL:\n{Sql}", mariadbQuery.Sql);

        // ─────────────────────────────────────────────────────────────────
        // 7. OracleDialect — Oracle Database: double quotes, :params, OFFSET/FETCH
        // ─────────────────────────────────────────────────────────────────
        var oracleDialect = OracleDialect.Default;
        var oracleQuery = oracleDialect.Render(queryModel);

        _logger.LogInformation("[Oracle] Dialect: {D}", oracleDialect.DialectName);
        _logger.LogInformation("[Oracle] SQL:\n{Sql}", oracleQuery.Sql);

        // ─────────────────────────────────────────────────────────────────
        // 8. PostgreSqlDialect with ILIKE (case-insensitive LIKE)
        // ─────────────────────────────────────────────────────────────────
        var pgILikeDialect = new PostgreSqlDialect(useCaseInsensitiveLike: true);

        var searchSpec = QuerySpec<Customer>.Empty
            .Where(c => c.Name.Contains("ali"));

        var searchModel = translator.Translate(searchSpec);
        var pgIlikeQuery = pgILikeDialect.Render(searchModel);

        _logger.LogInformation("[PostgreSQL ILIKE] SQL: {Sql}", pgIlikeQuery.Sql);

        // ─────────────────────────────────────────────────────────────────
        // 9. VerbatimColumnNameResolver — for columns exactly matching C# property names
        // ─────────────────────────────────────────────────────────────────
        var verbatimTranslator = new QuerySpecTranslator<Customer>(
            tableName: "Customers",
            columnNameResolver: VerbatimColumnNameResolver.Default);

        var verbatimModel = verbatimTranslator.Translate(
            QuerySpec<Customer>.Empty.Where(c => c.IsActive));

        var verbatimQuery = pgDialect.Render(verbatimModel);
        _logger.LogInformation("[VerbatimResolver] SQL: {Sql}", verbatimQuery.Sql);

        // ─────────────────────────────────────────────────────────────────
        // 10. ISqlDialect.QuoteIdentifier — identifier quoting helper
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[QuoteIdentifier] PG: {P}  MSSQL: {M}  SQLite: {S}  MySQL: {My}  MariaDB: {Ma}  Oracle: {O}",
            pgDialect.QuoteIdentifier("my_table"),
            mssqlDialect.QuoteIdentifier("my_table"),
            sqliteDialect.QuoteIdentifier("my_table"),
            mysqlDialect.QuoteIdentifier("my_table"),
            mariadbDialect.QuoteIdentifier("my_table"),
            oracleDialect.QuoteIdentifier("my_table"));

        // ─────────────────────────────────────────────────────────────────
        // 11. SqlBinaryOperator and AST Nodes
        //     QueryModel contains abstract syntax tree nodes:
        //     BinaryPredicateNode, InPredicateNode, BetweenPredicateNode,
        //     FullTextPredicateNode, RangePredicateNode, AndPredicateNode,
        //     OrPredicateNode, NotPredicateNode, SqlOrderNode, SqlParameter.
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[SqlBinaryOperator] Values: {Vals}",
            string.Join(", ", Enum.GetNames<SqlBinaryOperator>()));

        // AST nodes creation:
        var binaryNode = new BinaryPredicateNode("is_active", SqlBinaryOperator.Equal, "p1");
        var inNode = new InPredicateNode("status", "p2");
        var betweenNode = new BetweenPredicateNode("total_purchases", "p3", "p4");
        var fullTextNode = new FullTextPredicateNode("name", "p5");
        var rangeNode = new RangePredicateNode("credit_limit", "p6", "p7");
        var andNode = new AndPredicateNode(binaryNode, betweenNode);
        var orNode = new OrPredicateNode(binaryNode, rangeNode);
        var notNode = new NotPredicateNode(binaryNode);
        var orderNode = new SqlOrderNode("total_purchases", OrderDirection.Descending);
        var paramNode = new SqlParameter("p1", true);

        // SqlPredicateNode — abstract base record for all predicate nodes
        SqlPredicateNode predicateBase = binaryNode;
        _logger.LogInformation("[SqlPredicateNode] Abstract base. Concrete type: {T}", predicateBase.GetType().Name);

        _logger.LogInformation("[AST Nodes] Created nodes: Binary, In, Between, FullText, Range, And, Or, Not, Order, Param.");
        _logger.LogInformation("[OrPredicateNode] Left: {L}  Right: {R}", orNode.Left.GetType().Name, orNode.Right.GetType().Name);
        _logger.LogInformation("[NotPredicateNode] Inner: {I}", notNode.Inner.GetType().Name);
        _logger.LogInformation("[SqlOrderNode] Column: {C}  Direction: {D}", orderNode.ColumnName, orderNode.Direction);
        _logger.LogInformation("[SqlParameter] Name: {N}  Value: {V}", paramNode.Name, paramNode.Value);

        // ─────────────────────────────────────────────────────────────────
        // 12. QueryPlanCache — Bounded LRU Cache for query plans
        // ─────────────────────────────────────────────────────────────────
        int initialCacheCount = QueryPlanCache.Count;
        _logger.LogInformation("[QueryPlanCache] Capacity: {Cap}, Current entries: {Cnt}",
            QueryPlanCache.Capacity, initialCacheCount);

        var sampleExpr = Spec.For<Customer>(c => c.IsActive).ToExpression();
        QueryPlanCache.SetPlan(sampleExpr, "customers", queryModel);

        if (QueryPlanCache.TryGetPlan(sampleExpr, "customers", out var cachedPlan))
        {
            _logger.LogInformation("[QueryPlanCache] Plan retrieved successfully for table '{T}'.", cachedPlan.TableName);
        }

        _logger.LogInformation("[QueryPlanCache.Clear] Available API: QueryPlanCache.Clear() clears all cached query plans.");

        // ─────────────────────────────────────────────────────────────────
        // 13. SqlQueryType enum — query type generated by dialect
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[SqlQueryType] Values: {Vals}",
            string.Join(", ", Enum.GetNames<SqlQueryType>()));

        // Count query model:
        var countModel = queryModel with { QueryType = SqlQueryType.Count, Orders = [], Skip = null, Take = null };
        var countQuery = pgDialect.Render(countModel);
        _logger.LogInformation("[SqlQueryType.Count] SQL: {Sql}", countQuery.Sql);

        // Exists query model:
        var existsModel = queryModel with { QueryType = SqlQueryType.Exists, Orders = [], Skip = null, Take = 1 };
        var existsQuery = pgDialect.Render(existsModel);
        _logger.LogInformation("[SqlQueryType.Exists] SQL: {Sql}", existsQuery.Sql);

        // ─────────────────────────────────────────────────────────────────
        // 14. QueryModel.TableAlias — table alias in generated SQL
        // ─────────────────────────────────────────────────────────────────
        var aliasedModel = queryModel with { TableAlias = "c" };
        var aliasedQuery = pgDialect.Render(aliasedModel);
        _logger.LogInformation("[QueryModel.TableAlias] SQL with alias: {Sql}", aliasedQuery.Sql);

        // ─────────────────────────────────────────────────────────────────
        // 15. Dapper integration overview (QuerySpecDapperExtensions)
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[Dapper] API: QueryAsync / QueryFirstOrDefaultAsync / CountAsync / AnyAsync");

        return Task.CompletedTask;
    }
}




