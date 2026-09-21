// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification.Result;
using EricksonLopez.Specification.Showcase.Domain;
using EricksonLopez.Specification.Sql;
using EricksonLopez.Specification.Sqlite;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Specification.Showcase.Levels;

/// <summary>
/// Level 6 — Error Handling.
/// Demonstrates potential runtime exceptions, how to detect and avoid them,
/// and recovery strategies.
/// Also illustrates the Result Pattern (<see cref="ReadRepositoryResultExtensions"/>):
/// <c>FirstOrDefaultResultAsync</c>, <c>ListResultAsync</c>,
/// <c>SingleOrDefaultResultAsync</c>, <c>GetByIdResultAsync</c>.
/// </summary>
public sealed class Level6_ErrorHandling : ILevel
{
    private readonly ILogger<Level6_ErrorHandling> _logger;

    /// <inheritdoc/>
    public string Name => "Level 6 — Error Handling";

    /// <inheritdoc/>
    public string Description => "SQL Translator limitations, unsupported expressions, pre-execution validation with HasCriteria/HasPagination, and the Result pattern.";

    /// <summary>
    /// Initializes a new instance of the level.
    /// </summary>
    /// <param name="logger">The logger used for output.</param>
    public Level6_ErrorHandling(ILogger<Level6_ErrorHandling> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("--- {Name} ---", Name);

        // ─────────────────────────────────────────────────────────────────
        // 1. NotSupportedException in SQL Translator
        //    QuerySpecTranslator translates simple expressions (member access,
        //    constants, comparisons, supported string methods). Complex methods
        //    throw NotSupportedException.
        // ─────────────────────────────────────────────────────────────────
        var unsupportedSpec = QuerySpec<Customer>.Empty
            .Where(c => string.IsNullOrEmpty(c.Email)); // string.IsNullOrEmpty is not natively mapped

        var translator = new QuerySpecTranslator<Customer>("customers");
        var dialect = SqliteDialect.Default;

        try
        {
            var model = translator.Translate(unsupportedSpec);
            var query = dialect.Render(model);
            _logger.LogWarning("[Translator] SQL: {Sql}", query.Sql);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning("[NotSupportedException] Translator cannot process expression: {Msg}", ex.Message);
            _logger.LogInformation("[Solution] Rewrite as: c => c.Email == null || c.Email == string.Empty");
        }

        // ─────────────────────────────────────────────────────────────────
        // 1b. Supported equivalent pattern:
        // ─────────────────────────────────────────────────────────────────
        var supportedSpec = QuerySpec<Customer>.Empty
            .Where(c => c.Email == null || c.Email == string.Empty);

        try
        {
            var model = translator.Translate(supportedSpec);
            var sqlQuery = dialect.Render(model);
            _logger.LogInformation("[Translator OK] SQL: {Sql}", sqlQuery.Sql);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError("[Unexpected error] {Msg}", ex.Message);
        }

        // ─────────────────────────────────────────────────────────────────
        // 2. ArgumentNullException — fast-fail input validation
        // ─────────────────────────────────────────────────────────────────
        try
        {
            Expression<Func<Customer, bool>> nullPredicate = null!;
            var _ = QuerySpec<Customer>.Empty.Where(nullPredicate);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning("[ArgumentNullException] {Msg}", ex.Message);
        }

        // ─────────────────────────────────────────────────────────────────
        // 3. ArgumentOutOfRangeException — page < 1 or pageSize < 1
        // ─────────────────────────────────────────────────────────────────
        try
        {
            var _ = QuerySpec<Customer>.Empty.Page(page: 0, pageSize: 10);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning("[ArgumentOutOfRangeException] Page < 1: {Msg}", ex.ParamName);
        }

        try
        {
            var _ = QuerySpec<Customer>.Empty.Skip(count: -1);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning("[ArgumentOutOfRangeException] Skip < 0: {Msg}", ex.ParamName);
        }

        // 3b. ArgumentException — lower bound > upper bound in Spec.Between
        try
        {
            var _ = Spec.Between<Customer, int>(c => c.TotalPurchases, lower: 50, upper: 10);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("[ArgumentException] Spec.Between lower > upper: {Msg}", ex.Message);
        }

        // ─────────────────────────────────────────────────────────────────
        // 4. In-Memory evaluation with ExpressionInterpreter (AOT-safe)
        // ─────────────────────────────────────────────────────────────────
        var spec = Spec.For<Customer>(c => c.IsActive && c.TotalPurchases > 5);
        var customer = new Customer { IsActive = true, TotalPurchases = 15 };

        bool result = spec.IsSatisfiedBy(customer);
        _logger.LogInformation("[IsSatisfiedBy] Interpreted evaluation: {R}", result);

        // ─────────────────────────────────────────────────────────────────
        // 5. Pre-execution inspection with QuerySpecExtensions
        // ─────────────────────────────────────────────────────────────────
        var risky = QuerySpec<Customer>.Empty
            .OrderBy(c => c.Name);

        if (!risky.HasCriteria())
            _logger.LogWarning("[Validation] WARNING: QuerySpec without filter criteria will return ALL records.");

        if (!risky.HasPagination())
            _logger.LogWarning("[Validation] WARNING: QuerySpec without pagination (Take) may produce an unbounded query.");

        if (risky.HasOrdering() && !risky.HasPagination())
            _logger.LogWarning("[Validation] WARNING: Sorting unbounded result sets can be expensive.");

        // ─────────────────────────────────────────────────────────────────
        // 6. Result Pattern integration (EricksonLopez.Specification.Result)
        // ─────────────────────────────────────────────────────────────────
        IReadRepository<Customer> repo = new MockCustomerRepository();

        // 6a. FirstOrDefaultResultAsync — null → Error.NotFound
        var firstResult = await repo.FirstOrDefaultResultAsync(
            QuerySpec<Customer>.Empty.Where(c => c.Name == "Alice"));
        _logger.LogInformation("[FirstOrDefaultResultAsync] Success: {S}  Value: {V}",
            firstResult.IsSuccess, firstResult.IsSuccess ? firstResult.Value!.Name : "N/A");

        var notFoundResult = await repo.FirstOrDefaultResultAsync(
            QuerySpec<Customer>.Empty.Where(c => c.Name == "NonExistent"));
        _logger.LogInformation("[FirstOrDefaultResultAsync NotFound] Success: {S}  Error: {E}",
            notFoundResult.IsSuccess, notFoundResult.IsFailure ? notFoundResult.Error.Code : "N/A");

        // 6b. ListResultAsync
        var listResult = await repo.ListResultAsync(
            QuerySpec<Customer>.Empty.Where(c => c.IsActive));
        _logger.LogInformation("[ListResultAsync] Success: {S}  Count: {N}",
            listResult.IsSuccess, listResult.IsSuccess ? listResult.Value!.Count : 0);

        // 6c. SingleOrDefaultResultAsync
        var singleResult = await repo.SingleOrDefaultResultAsync(
            QuerySpec<Customer>.Empty.Where(c => c.Name == "Alice"));
        _logger.LogInformation("[SingleOrDefaultResultAsync] Success: {S}  Name: {N}",
            singleResult.IsSuccess, singleResult.IsSuccess ? singleResult.Value!.Name : "N/A");

        // 6d. GetByIdResultAsync
        var byIdResult = await repo.GetByIdResultAsync<Customer, System.Guid>(System.Guid.Empty);
        _logger.LogInformation("[GetByIdResultAsync] Success: {S}  Error: {E}",
            byIdResult.IsSuccess, byIdResult.IsFailure ? byIdResult.Error.Code : "N/A");

        // ─────────────────────────────────────────────────────────────────
        // 7. Error Handling Summary
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[Summary] Domain specifications are pure. I/O and SQL generation errors belong to the infrastructure layer.");
        _logger.LogInformation("[Result Pattern] ReadRepositoryResultExtensions transforms null/exceptions into typed Results cleanly.");
    }
}





