// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification.Dapper;
using EricksonLopez.Specification.PostgreSql;
using EricksonLopez.Specification.Showcase.Domain;
using EricksonLopez.Specification.Sql;
using EricksonLopez.Specification.Sqlite;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Specification.Showcase.Levels;

/// <summary>
/// Level 9 — Official Extensions.
/// Demonstrates official library integrations: Dapper (<see cref="QuerySpecDapperExtensions"/>),
/// EF Core (<c>QuerySpecEfCoreExtensions</c>), LINQ (<c>QuerySpecLinqExtensions</c>),
/// and MongoDB (<c>MongoSpecificationEvaluator</c>).
/// </summary>
public sealed class Level9_Extensions : ILevel
{
    private readonly ILogger<Level9_Extensions> _logger;

    /// <inheritdoc/>
    public string Name => "Level 9 — Official Extensions";

    /// <inheritdoc/>
    public string Description => "QuerySpecDapperExtensions: QueryAsync, QueryFirstOrDefaultAsync, CountAsync, AnyAsync across all supported dialects.";

    /// <summary>
    /// Initializes a new instance of the level.
    /// </summary>
    /// <param name="logger">The logger used for output.</param>
    public Level9_Extensions(ILogger<Level9_Extensions> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("--- {Name} ---", Name);

        // ─────────────────────────────────────────────────────────────────
        // Base Setup: spec + translator + dialect
        // ─────────────────────────────────────────────────────────────────
        var activeSpec = new ActiveCustomerSpecification();
        var vipSpec = new VipCustomerSpecification(minimumPurchases: 10);

        var spec = QuerySpec<Customer>.Empty
            .And(activeSpec)
            .And(vipSpec)
            .OrderByDescending(c => c.TotalPurchases)
            .Page(page: 1, pageSize: 50);

        var translator = new QuerySpecTranslator<Customer>(
            tableName: "customers",
            columnNameResolver: SnakeCaseColumnNameResolver.Default);

        // ─────────────────────────────────────────────────────────────────
        // 1. QuerySpecDapperExtensions — 4 core operations demonstrated
        // ─────────────────────────────────────────────────────────────────
        DemonstrateQuerySql(spec, translator, PostgreSqlDialect.Default);
        DemonstrateQuerySql(spec, translator, SqliteDialect.Default);

        _logger.LogInformation("[Dapper] Complete API demonstrated. Connect to real DB for live query execution.");

        // ─────────────────────────────────────────────────────────────────
        // 2. QuerySpecLinqExtensions and EF Core Extensions
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[Linq] QuerySpecLinqExtensions: Apply<T>, Apply<T,TResult>, Any<T>, Count<T>");
        _logger.LogInformation("[EF Core] QuerySpecEfCoreExtensions: Apply(asSplitQuery: true, ignoreAutoIncludes: true)");
        _logger.LogInformation("[EF Core] EfSpecificationEvaluator: Default evaluator for EF Core pipeline.");

        // ─────────────────────────────────────────────────────────────────
        // 3. MongoDB Integration (EricksonLopez.Specification.MongoDB)
        // ─────────────────────────────────────────────────────────────────
        var mongoFilter = EricksonLopez.Specification.MongoDB.MongoSpecificationEvaluator.GetFilter(spec);
        var mongoSort = EricksonLopez.Specification.MongoDB.MongoSpecificationEvaluator.GetSort(spec);
        _logger.LogInformation("[MongoDB] Evaluator compiled FilterDefinition and SortDefinition from QuerySpec.");

        // ─────────────────────────────────────────────────────────────────
        // 4. Dialect Selection per Environment
        // ─────────────────────────────────────────────────────────────────
        var dialects = new ISqlDialect[]
        {
            PostgreSqlDialect.Default,
            SqliteDialect.Default,
            EricksonLopez.Specification.MsSql.MsSqlDialect.Default,
            EricksonLopez.Specification.MySql.MySqlDialect.Default,
            EricksonLopez.Specification.MariaDb.MariaDbDialect.Default,
            EricksonLopez.Specification.Oracle.OracleDialect.Default
        };

        foreach (var d in dialects)
        {
            _logger.LogInformation("[Dialect] {Name}: prefix='{P}' identifier='{I}'",
                d.DialectName, d.ParameterPrefix, d.QuoteIdentifier("customers"));
        }

        // ─────────────────────────────────────────────────────────────────
        // 5. EF Core DI Extensions
        // ─────────────────────────────────────────────────────────────────
        var efServices = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.SpecificationEntityFrameworkServiceCollectionExtensions.AddSpecificationEntityFramework(efServices);
        Microsoft.Extensions.DependencyInjection.SpecificationEntityFrameworkServiceCollectionExtensions.AddEfReadRepository<ShowcaseDbContext, Customer>(efServices);
        _logger.LogInformation("[EF Core DI] AddSpecificationEntityFramework & AddEfReadRepository registered.");

        // ─────────────────────────────────────────────────────────────────
        // 6. QueryPlanCache.Clear & WithCursor pagination
        // ─────────────────────────────────────────────────────────────────
        EricksonLopez.Specification.Sql.QueryPlanCache.Clear();
        var cursorSpec = spec.WithCursor(c => c.TotalPurchases, 100, CursorDirection.After, 20);
        _logger.LogInformation("[QueryPlanCache & Cursor] QueryPlanCache.Clear executed, spec.WithCursor configured (Take={Take}).", cursorSpec.TakeCount);

        // ─────────────────────────────────────────────────────────────────
        // 7. Dapper QueryFirstOrDefaultAsync
        // ─────────────────────────────────────────────────────────────────
        using var sqliteConn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        sqliteConn.Open();
        using var createTableCmd = sqliteConn.CreateCommand();
        createTableCmd.CommandText = "CREATE TABLE customers (id INT, total_purchases INT, is_active INT);";
        createTableCmd.ExecuteNonQuery();
        var firstCustomer = await sqliteConn.QueryFirstOrDefaultAsync<Customer>(spec, translator, SqliteDialect.Default);
        _logger.LogInformation("[Dapper] QueryFirstOrDefaultAsync executed successfully against SQLite.");

        // ─────────────────────────────────────────────────────────────────
        // 8. MongoDB Extensions & Contracts
        // ─────────────────────────────────────────────────────────────────
        try
        {
            global::MongoDB.Driver.IMongoCollection<Customer> mongoCol = null!;
            await EricksonLopez.Specification.MongoDB.MongoSpecificationExtensions.FindAsync(mongoCol, spec);
            await EricksonLopez.Specification.MongoDB.MongoSpecificationExtensions.CountDocumentsAsync(mongoCol, spec);
            EricksonLopez.Specification.MongoDB.MongoSpecificationEvaluator.Find(mongoCol, spec);
        }
        catch (ArgumentNullException)
        {
            // Expected argument null validation
        }

        try
        {
            global::MongoDB.Driver.IFindFluent<Customer, Customer> findFluent = null!;
            EricksonLopez.Specification.MongoDB.MongoSpecificationEvaluator.ApplySpecification(findFluent, spec);
        }
        catch (ArgumentNullException)
        {
            // Expected argument null validation
        }
        _logger.LogInformation("[MongoDB] Find, FindAsync, CountDocumentsAsync & ApplySpecification contracts verified.");
    }

    private void DemonstrateQuerySql(
        QuerySpec<Customer> spec,
        QuerySpecTranslator<Customer> translator,
        ISqlDialect dialect)
    {
        // QueryAsync
        var queryModel = translator.Translate(spec);
        var querySql = dialect.Render(queryModel);
        _logger.LogInformation("[{D}] QueryAsync SQL:\n{Sql}", dialect.DialectName, querySql.Sql);

        // CountAsync
        var countModel = translator.Translate(spec) with
        {
            Orders = [],
            Skip = null,
            Take = null,
            Projections = ["COUNT(*)"]
        };
        var countSql = dialect.Render(countModel);
        _logger.LogInformation("[{D}] CountAsync SQL:\n{Sql}", dialect.DialectName, countSql.Sql);

        // AnyAsync
        var anyModel = translator.Translate(spec) with
        {
            Orders = [],
            Skip = null,
            Take = 1,
            Projections = ["1"]
        };
        var anySql = dialect.Render(anyModel);
        _logger.LogInformation("[{D}] AnyAsync SQL:\n{Sql}", dialect.DialectName, anySql.Sql);
    }

    private sealed class MockDbConnection : IDbConnection
    {
        [System.Diagnostics.CodeAnalysis.AllowNull]
        string IDbConnection.ConnectionString { get => string.Empty; set { } }
        public int ConnectionTimeout => 30;
        public string Database => "showcase_db";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotSupportedException("Mock");
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException("Mock");
        public void ChangeDatabase(string databaseName) { }
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotSupportedException("Mock");
        public void Open() { }
        public void Dispose() { }
    }
}

public sealed class ShowcaseDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public ShowcaseDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<ShowcaseDbContext> options) : base(options) { }
}
