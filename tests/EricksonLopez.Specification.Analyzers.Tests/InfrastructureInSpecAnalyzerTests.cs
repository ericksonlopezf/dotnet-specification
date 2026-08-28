// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EricksonLopez.Specification.Analyzers.Tests;

public sealed class InfrastructureInSpecAnalyzerTests
{
    [Fact]
    public void Descriptor_HasCorrectIdAndSeverity()
    {
        var descriptor = SpecificationDiagnosticDescriptors.InfrastructureServiceInSpecification;
        descriptor.Id.Should().Be("SPEC008");
        descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Analyzer_ValidSpecification_NoDiagnostic()
    {
        var code = """
            using EricksonLopez.Specification;
            
            public class CustomerSpec : Specification<string>
            {
                public CustomerSpec(int age, string name) { }
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<InfrastructureInSpecAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_InjectedIServiceProvider_ReportsDiagnostic()
    {
        var code = """
            
            public class CustomerSpec : Specification<string>
            {
                public CustomerSpec(int age, IServiceProvider {|#0:provider|}) { }
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        var expected = new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("provider", "IServiceProvider", "CustomerSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<InfrastructureInSpecAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_InjectedAllInfrastructureTypes_ReportsDiagnostic()
    {
        var code = """
            
            public class SomeContext { }
            public class SomeService { }
            public class SomeClient { }
            public class SomeManager { }
            public class SomeProvider { }
            public class SomeFactory { }
            public class SomeSender { }
            public class SomeDispatcher { }
            public class SomeBus { }
            public class SomeStore { }
            public class SomeCache { }
            public class SomeDao { }
            
            public interface IServiceScopeFactory { }
            public class HttpClient { }
            public interface IHttpClientFactory { }
            public interface ILogger { }
            public interface ILoggerFactory { }
            public class DbContext { }
            public class Repository { }

            public class CustomerSpec : Specification<string>
            {
                public CustomerSpec(
                    SomeContext {|#0:p0|},
                    SomeService {|#1:p1|},
                    SomeClient {|#2:p2|},
                    SomeManager {|#3:p3|},
                    SomeProvider {|#4:p4|},
                    SomeFactory {|#5:p5|},
                    SomeSender {|#6:p6|},
                    SomeDispatcher {|#7:p7|},
                    SomeBus {|#8:p8|},
                    SomeStore {|#9:p9|},
                    SomeCache {|#10:p10|},
                    SomeDao {|#11:p11|},
                    IServiceScopeFactory {|#12:p12|},
                    HttpClient {|#13:p13|},
                    IHttpClientFactory {|#14:p14|},
                    ILogger {|#15:p15|},
                    ILoggerFactory {|#16:p16|},
                    DbContext {|#17:p17|},
                    Repository {|#18:p18|}) { }

                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        var expected = new[]
        {
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("p0", "SomeContext", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(1).WithArguments("p1", "SomeService", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(2).WithArguments("p2", "SomeClient", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(3).WithArguments("p3", "SomeManager", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(4).WithArguments("p4", "SomeProvider", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(5).WithArguments("p5", "SomeFactory", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(6).WithArguments("p6", "SomeSender", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(7).WithArguments("p7", "SomeDispatcher", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(8).WithArguments("p8", "SomeBus", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(9).WithArguments("p9", "SomeStore", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(10).WithArguments("p10", "SomeCache", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(11).WithArguments("p11", "SomeDao", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(12).WithArguments("p12", "IServiceScopeFactory", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(13).WithArguments("p13", "HttpClient", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(14).WithArguments("p14", "IHttpClientFactory", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(15).WithArguments("p15", "ILogger", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(16).WithArguments("p16", "ILoggerFactory", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(17).WithArguments("p17", "DbContext", "CustomerSpec"),
            new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning).WithLocation(18).WithArguments("p18", "Repository", "CustomerSpec")
        };

        await AnalyzerTestHelper.VerifyAnalyzerAsync<InfrastructureInSpecAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_NotInheritingSpecification_NoDiagnostic()
    {
        var code = """
            
            // This is just a normal class, so injecting IServiceProvider is fine
            public class CustomerService
            {
                public CustomerService(IServiceProvider provider) { }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<InfrastructureInSpecAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_BaseTypeIsDbContext_ReportsDiagnostic()
    {
        var code = """
            
            namespace Microsoft.EntityFrameworkCore 
            {
                public class DbContext { }
            }
            
            public class MyDatabase : Microsoft.EntityFrameworkCore.DbContext { }

            public class CustomerSpec : Specification<string>
            {
                public CustomerSpec(MyDatabase {|#0:db|}) { }
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        var expected = new DiagnosticResult("SPEC008", DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("db", "MyDatabase", "CustomerSpec");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<InfrastructureInSpecAnalyzer>(code, expected);
    }

    [Fact]
    public async Task Analyzer_FakeDbContextBase_NoDiagnostic()
    {
        var code = """
            
            namespace Fake 
            {
                public class DbContext { }
            }
            
            public class MyDatabase : Fake.DbContext { }

            public class CustomerSpec : Specification<string>
            {
                public CustomerSpec(MyDatabase db) { }
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<InfrastructureInSpecAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_OtherEfCoreTypeBase_NoDiagnostic()
    {
        var code = """
            
            namespace Microsoft.EntityFrameworkCore 
            {
                public class ModelBuilder { }
            }
            
            public class MyDatabase : Microsoft.EntityFrameworkCore.ModelBuilder { }

            public class CustomerSpec : Specification<string>
            {
                public CustomerSpec(MyDatabase db) { }
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<InfrastructureInSpecAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_InheritsFromFakeSpecificationWithDifferentNamespace_NoDiagnostic()
    {
        var code = """
            
            namespace FakeNamespace
            {
                public class Specification<T> { }
            }

            public class CustomerSpec : FakeNamespace.Specification<string>
            {
                public CustomerSpec(IServiceProvider provider) { }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<InfrastructureInSpecAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_InheritsFromNonGenericSpecification_NoDiagnostic()
    {
        var code = """
            
            namespace EricksonLopez.Specification
            {
                public class Specification { }
            }

            public class CustomerSpec : EricksonLopez.Specification.Specification
            {
                public CustomerSpec(IServiceProvider provider) { }
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<InfrastructureInSpecAnalyzer>(code);
    }

    [Fact]
    public async Task Analyzer_GlobalNamespaceDbContextBase_NoDiagnostic()
    {
        var code = """
            
            public class DbContext { }
            
            public class MyDatabase : DbContext { }

            public class CustomerSpec : Specification<string>
            {
                public CustomerSpec(MyDatabase db) { }
                public override System.Linq.Expressions.Expression<System.Func<string, bool>> BuildExpression() => x => true;
            }
            """;

        await AnalyzerTestHelper.VerifyAnalyzerAsync<InfrastructureInSpecAnalyzer>(code);
    }
}





