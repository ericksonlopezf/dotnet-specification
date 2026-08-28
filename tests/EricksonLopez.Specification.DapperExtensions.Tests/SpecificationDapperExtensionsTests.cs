// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.UnitOfWork;
using EricksonLopez.Specification;
using EricksonLopez.Specification.Dapper.Tests;
using EricksonLopez.Specification.Sql;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Specification.DapperExtensions.Tests;

public sealed class SpecificationDapperExtensionsTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDbTransaction _transaction = Substitute.For<IDbTransaction>();
    private readonly IDbConnection _connection = Substitute.For<IDbConnection>();
    private readonly ISqlDialect _dialect = Substitute.For<ISqlDialect>();

    public SpecificationDapperExtensionsTests()
    {
        _transaction.Connection.Returns(_connection);
        _unitOfWork.Transaction.Returns(_transaction);
        _dialect.ParameterPrefix.Returns("@");
        _dialect.QuoteIdentifier(Arg.Any<string>()).Returns(x => $"\"{x.Arg<string>()}\"");
        _dialect.Render(Arg.Any<QueryModel>()).Returns(new SqlQuery { Sql = "SELECT * FROM \"Products\"", Parameters = new Dictionary<string, object?>() });
    }

    private sealed class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
    }

    #region Argument Validation Tests

    private static IUnitOfWork CreateThrowingUnitOfWork()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.Transaction.Returns(_ => throw new InvalidOperationException("Transaction must not be accessed during argument validation."));
        return uow;
    }

    [Fact]
    public async Task QueryAsync_WhenUnitOfWorkNull_ThrowsArgumentNullException()
    {
        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");

        Func<Task> act = async () => await ((IUnitOfWork)null!).QueryAsync(spec, translator, _dialect);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public async Task QueryAsync_WhenSpecNull_ThrowsArgumentNullException()
    {
        var translator = new QuerySpecTranslator<Product>("Products");
        var throwingUow = CreateThrowingUnitOfWork();

        Func<Task> act = async () => await throwingUow.QueryAsync<Product>(null!, translator, _dialect);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("spec");
    }

    [Fact]
    public async Task QueryAsync_WhenTranslatorNull_ThrowsArgumentNullException()
    {
        var spec = new QuerySpec<Product>();
        var throwingUow = CreateThrowingUnitOfWork();

        Func<Task> act = async () => await throwingUow.QueryAsync<Product>(spec, null!, _dialect);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("translator");
    }

    [Fact]
    public async Task QueryAsync_WhenDialectNull_ThrowsArgumentNullException()
    {
        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");
        var throwingUow = CreateThrowingUnitOfWork();

        Func<Task> act = async () => await throwingUow.QueryAsync<Product>(spec, translator, null!);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("dialect");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenUnitOfWorkNull_ThrowsArgumentNullException()
    {
        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");

        Func<Task> act = async () => await ((IUnitOfWork)null!).QueryFirstOrDefaultAsync(spec, translator, _dialect);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenSpecNull_ThrowsArgumentNullException()
    {
        var translator = new QuerySpecTranslator<Product>("Products");
        var throwingUow = CreateThrowingUnitOfWork();

        Func<Task> act = async () => await throwingUow.QueryFirstOrDefaultAsync<Product>(null!, translator, _dialect);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("spec");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenTranslatorNull_ThrowsArgumentNullException()
    {
        var spec = new QuerySpec<Product>();
        var throwingUow = CreateThrowingUnitOfWork();

        Func<Task> act = async () => await throwingUow.QueryFirstOrDefaultAsync<Product>(spec, null!, _dialect);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("translator");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenDialectNull_ThrowsArgumentNullException()
    {
        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");
        var throwingUow = CreateThrowingUnitOfWork();

        Func<Task> act = async () => await throwingUow.QueryFirstOrDefaultAsync<Product>(spec, translator, null!);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("dialect");
    }

    [Fact]
    public async Task CountAsync_WhenUnitOfWorkNull_ThrowsArgumentNullException()
    {
        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");

        Func<Task> act = async () => await ((IUnitOfWork)null!).CountAsync(spec, translator, _dialect);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public async Task CountAsync_WhenSpecNull_ThrowsArgumentNullException()
    {
        var translator = new QuerySpecTranslator<Product>("Products");
        var throwingUow = CreateThrowingUnitOfWork();

        Func<Task> act = async () => await throwingUow.CountAsync<Product>(null!, translator, _dialect);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("spec");
    }

    [Fact]
    public async Task CountAsync_WhenTranslatorNull_ThrowsArgumentNullException()
    {
        var spec = new QuerySpec<Product>();
        var throwingUow = CreateThrowingUnitOfWork();

        Func<Task> act = async () => await throwingUow.CountAsync<Product>(spec, null!, _dialect);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("translator");
    }

    [Fact]
    public async Task CountAsync_WhenDialectNull_ThrowsArgumentNullException()
    {
        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");
        var throwingUow = CreateThrowingUnitOfWork();

        Func<Task> act = async () => await throwingUow.CountAsync(spec, translator, null!);
        (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).Which.ParamName.Should().Be("dialect");
    }

    #endregion

    #region Connection Null Checks

    [Fact]
    public async Task QueryAsync_WhenTransactionConnectionNull_ThrowsInvalidOperationException()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var tx = Substitute.For<IDbTransaction>();
        tx.Connection.Returns((IDbConnection?)null);
        uow.Transaction.Returns(tx);

        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");

        Func<Task> act = async () => await uow.QueryAsync(spec, translator, _dialect);
        await act.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("The UnitOfWork transaction has no associated connection.");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenTransactionConnectionNull_ThrowsInvalidOperationException()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var tx = Substitute.For<IDbTransaction>();
        tx.Connection.Returns((IDbConnection?)null);
        uow.Transaction.Returns(tx);

        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");

        Func<Task> act = async () => await uow.QueryFirstOrDefaultAsync(spec, translator, _dialect);
        await act.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("The UnitOfWork transaction has no associated connection.");
    }

    [Fact]
    public async Task CountAsync_WhenTransactionConnectionNull_ThrowsInvalidOperationException()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var tx = Substitute.For<IDbTransaction>();
        tx.Connection.Returns((IDbConnection?)null);
        uow.Transaction.Returns(tx);

        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");

        Func<Task> act = async () => await uow.CountAsync(spec, translator, _dialect);
        await act.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("The UnitOfWork transaction has no associated connection.");
    }

    #endregion

    #region Happy Path & Execution Tests

    [Fact]
    public async Task QueryAsync_ValidSpecification_ExecutesAndReturnsResults()
    {
        var fakeConn = new FakeDbConnection
        {
            DataReaderRows =
            [
                new() { ["Id"] = 1, ["Name"] = "Laptop", ["IsActive"] = true, ["Price"] = 1200m },
                new() { ["Id"] = 2, ["Name"] = "Mouse", ["IsActive"] = true, ["Price"] = 25m }
            ]
        };
        var fakeTx = new FakeDbTransaction(fakeConn);
        var uow = Substitute.For<IUnitOfWork>();
        uow.Transaction.Returns(fakeTx);

        var spec = new QuerySpec<Product>().Where(p => p.IsActive);
        var translator = new QuerySpecTranslator<Product>("Products");

        var results = (await uow.QueryAsync(spec, translator, _dialect, commandTimeout: 45)).ToList();

        results.Should().HaveCount(2);
        results[0].Name.Should().Be("Laptop");
        results[1].Name.Should().Be("Mouse");

        fakeConn.LastCommand.Should().NotBeNull();
        fakeConn.LastCommand!.CommandTimeout.Should().Be(45);
        fakeConn.LastCommand.Transaction.Should().BeSameAs(fakeTx);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WithMatchingRows_ReturnsFirstEntity()
    {
        var fakeConn = new FakeDbConnection
        {
            DataReaderRows =
            [
                new() { ["Id"] = 1, ["Name"] = "Laptop", ["IsActive"] = true, ["Price"] = 1200m }
            ]
        };
        var fakeTx = new FakeDbTransaction(fakeConn);
        var uow = Substitute.For<IUnitOfWork>();
        uow.Transaction.Returns(fakeTx);

        var spec = new QuerySpec<Product>().Where(p => p.Id == 1);
        var translator = new QuerySpecTranslator<Product>("Products");

        var result = await uow.QueryFirstOrDefaultAsync(spec, translator, _dialect, commandTimeout: 60);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Laptop");

        fakeConn.LastCommand.Should().NotBeNull();
        fakeConn.LastCommand!.CommandTimeout.Should().Be(60);
        fakeConn.LastCommand.Transaction.Should().BeSameAs(fakeTx);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenNoRows_ReturnsNull()
    {
        var fakeConn = new FakeDbConnection { DataReaderRows = [] };
        var fakeTx = new FakeDbTransaction(fakeConn);
        var uow = Substitute.For<IUnitOfWork>();
        uow.Transaction.Returns(fakeTx);

        var spec = new QuerySpec<Product>().Where(p => p.Id == 999);
        var translator = new QuerySpecTranslator<Product>("Products");

        var result = await uow.QueryFirstOrDefaultAsync(spec, translator, _dialect);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CountAsync_ValidSpecification_ReturnsScalarCount()
    {
        var fakeConn = new FakeDbConnection { ScalarResult = 7 };
        var fakeTx = new FakeDbTransaction(fakeConn);
        var uow = Substitute.For<IUnitOfWork>();
        uow.Transaction.Returns(fakeTx);

        var spec = new QuerySpec<Product>().Where(p => p.IsActive).OrderBy(p => p.Name).Page(1, 10);
        var translator = new QuerySpecTranslator<Product>("Products");

        var count = await uow.CountAsync(spec, translator, _dialect, commandTimeout: 15);

        count.Should().Be(7);
        fakeConn.LastCommand.Should().NotBeNull();
        fakeConn.LastCommand!.CommandTimeout.Should().Be(15);
        fakeConn.LastCommand.Transaction.Should().BeSameAs(fakeTx);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task QueryAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var fakeConn = new FakeDbConnection();
        var fakeTx = new FakeDbTransaction(fakeConn);
        var uow = Substitute.For<IUnitOfWork>();
        uow.Transaction.Returns(fakeTx);

        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = async () => await uow.QueryAsync(spec, translator, _dialect, cancellationToken: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var fakeConn = new FakeDbConnection();
        var fakeTx = new FakeDbTransaction(fakeConn);
        var uow = Substitute.For<IUnitOfWork>();
        uow.Transaction.Returns(fakeTx);

        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = async () => await uow.QueryFirstOrDefaultAsync(spec, translator, _dialect, cancellationToken: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CountAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var fakeConn = new FakeDbConnection();
        var fakeTx = new FakeDbTransaction(fakeConn);
        var uow = Substitute.For<IUnitOfWork>();
        uow.Transaction.Returns(fakeTx);

        var spec = new QuerySpec<Product>();
        var translator = new QuerySpecTranslator<Product>("Products");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = async () => await uow.CountAsync(spec, translator, _dialect, cancellationToken: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}
