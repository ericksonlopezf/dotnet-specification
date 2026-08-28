// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification.Showcase.Domain;
using EricksonLopez.Specification.Sql;
using EricksonLopez.Specification.Sqlite;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Specification.Showcase.Levels;

/// <summary>
/// Level 8 — Customization.
/// Demonstrates extending the library by implementing its public interfaces:
/// <see cref="IColumnNameResolver"/> and <see cref="ISqlDialect"/>.
/// </summary>
public sealed class Level8_Customization : ILevel
{
    private readonly ILogger<Level8_Customization> _logger;

    /// <inheritdoc/>
    public string Name => "Level 8 — Customization";

    /// <inheritdoc/>
    public string Description => "Custom implementations of IColumnNameResolver and ISqlDialect. Extension without modifying core library.";

    /// <summary>
    /// Initializes a new instance of the level.
    /// </summary>
    /// <param name="logger">The logger used for output.</param>
    public Level8_Customization(ILogger<Level8_Customization> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync()
    {
        _logger.LogInformation("--- {Name} ---", Name);

        // ─────────────────────────────────────────────────────────────────
        // 1. IColumnNameResolver — multiple implementations
        //    SnakeCaseColumnNameResolver.Default: IsActive → is_active
        //    VerbatimColumnNameResolver.Default:  IsActive → IsActive
        //    LegacyDbColumnNameResolver (custom): IsActive → TBL_COL_ISACTIVE
        //    ExplicitMappingColumnNameResolver:   explicit dictionary mapping
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[IColumnNameResolver] Comparing resolvers:");

        var snake = SnakeCaseColumnNameResolver.Default;
        var verbatim = VerbatimColumnNameResolver.Default;
        var legacy = new LegacyDbColumnNameResolver();
        var explicit_ = new ExplicitMappingColumnNameResolver(
            new Dictionary<string, string>
            {
                { "IsActive",       "ACTIVO"        },
                { "TotalPurchases", "TOT_COMPRAS"   },
                { "Name",           "NOMBRE"        }
            });

        string[] props = ["IsActive", "TotalPurchases", "Name", "CreditLimit"];

        foreach (var prop in props)
        {
            _logger.LogInformation(
                "  {Prop,-20} | Snake: {S,-25} | Verbatim: {V,-20} | Legacy: {L,-25} | Explicit: {E}",
                prop,
                snake.Resolve(prop),
                verbatim.Resolve(prop),
                legacy.Resolve(prop),
                explicit_.Resolve(prop));
        }

        // ─────────────────────────────────────────────────────────────────
        // 2. Custom resolver usage with QuerySpecTranslator
        // ─────────────────────────────────────────────────────────────────
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => c.TotalPurchases > 5)
            .OrderByDescending(c => c.TotalPurchases)
            .Take(10);

        var legacyTranslator = new QuerySpecTranslator<Customer>("CUSTOMERS_TABLE", legacy);
        var legacyModel = legacyTranslator.Translate(spec);
        var legacyQuery = SqliteDialect.Default.Render(legacyModel);

        _logger.LogInformation("[LegacyResolver] SQL: {Sql}", legacyQuery.Sql);

        var verbatimTranslator = new QuerySpecTranslator<Customer>("Customers", verbatim);
        var verbatimModel = verbatimTranslator.Translate(spec);
        var verbatimQuery = SqliteDialect.Default.Render(verbatimModel);

        _logger.LogInformation("[VerbatimResolver] SQL: {Sql}", verbatimQuery.Sql);

        // ─────────────────────────────────────────────────────────────────
        // 3. ISqlDialect — full custom implementation
        // ─────────────────────────────────────────────────────────────────
        var legacyDialect = new LegacyReportingDialect();
        var standardTranslator = new QuerySpecTranslator<Customer>("customers");
        var model = standardTranslator.Translate(spec);
        var legacySql = legacyDialect.Render(model);

        _logger.LogInformation("[ISqlDialect Custom] Dialect: {D}", legacyDialect.DialectName);
        _logger.LogInformation("[ISqlDialect Custom] SQL: {Sql}", legacySql.Sql);
        _logger.LogInformation("[ISqlDialect Custom] Parameter prefix: '{P}'", legacyDialect.ParameterPrefix);
        _logger.LogInformation("[ISqlDialect Custom] QuoteIdentifier: '{Q}'", legacyDialect.QuoteIdentifier("my_table"));

        // ─────────────────────────────────────────────────────────────────
        // 4. Custom Parameterized Specifications
        // ─────────────────────────────────────────────────────────────────
        var vip10 = new VipCustomerSpecification(minimumPurchases: 10);
        var vip50 = new VipCustomerSpecification(minimumPurchases: 50);
        var recent = new RecentCustomerSpecification(daysBack: 7);
        var highCr = new HighCreditCustomerSpecification(minimumCreditLimit: 5_000m);

        _logger.LogInformation("[Custom Specs] VIP(10), VIP(50), Recent(7d), HighCredit(5000) parameterized specifications created.");

        var combinedSpec = vip10.And(recent).And(highCr).Not();
        _logger.LogInformation("[Composition] Combined (VIP10 && Recent && HighCredit).Not(): {E}", combinedSpec.ToExpression());

        return Task.CompletedTask;
    }

    private sealed class LegacyDbColumnNameResolver : IColumnNameResolver
    {
        public string Resolve(string propertyName)
            => "TBL_COL_" + propertyName.ToUpperInvariant();
    }

    private sealed class ExplicitMappingColumnNameResolver : IColumnNameResolver
    {
        private readonly Dictionary<string, string> _map;

        public ExplicitMappingColumnNameResolver(Dictionary<string, string> map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public string Resolve(string propertyName)
            => _map.TryGetValue(propertyName, out var col) ? col : propertyName;
    }

    private sealed class LegacyReportingDialect : ISqlDialect
    {
        public string DialectName => "LegacyReporting";

        public string ParameterPrefix => ":";

        public string QuoteIdentifier(string identifier)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            return identifier.ToUpperInvariant();
        }

        public SqlQuery Render(QueryModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var sql = new StringBuilder(128);
            var parameters = new Dictionary<string, object?>();

            foreach (var p in model.Parameters)
                parameters[p.Name] = p.Value;

            sql.Append("SELECT * FROM ");
            sql.Append(QuoteIdentifier(model.TableName));

            if (!model.Filters.IsEmpty)
            {
                sql.Append(" WHERE ");
                sql.Append("/* FILTERS APPLIED */");
            }

            if (model.Take.HasValue)
            {
                sql.Insert(0, "SELECT * FROM (");
                sql.Append($") WHERE ROWNUM <= :{model.Parameters.Length + 1}");
                parameters[$"p{model.Parameters.Length + 1}"] = model.Take.Value;
            }

            return new SqlQuery { Sql = sql.ToString(), Parameters = parameters };
        }
    }
}




