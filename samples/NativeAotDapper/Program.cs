// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Specification;
using EricksonLopez.Specification.Dapper;
using EricksonLopez.Specification.PostgreSql;
using EricksonLopez.Specification.Sql;
using NativeAotDapper.Specs;
using Npgsql;

[assembly: DapperAot]

namespace NativeAotDapper;

internal static class Program
{
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "Sample app handles known closures.")]
    internal static async Task Main(string[] args)
    {
        Console.WriteLine("EricksonLopez.Specification NativeAOT + Dapper Sample");
        Console.WriteLine("-----------------------------------------------------");

        // 1. Create Domain Specification
        var activePremiumUsSpec = new ActiveCustomerSpec()
            .And(new PremiumCustomerSpec("US"));

        Console.WriteLine("\n[1] Domain validation (In-Memory, AOT Safe):");
        var testCustomer = new Customer { Name = "John", IsActive = true, Tier = "Premium", Region = "US" };

        // This uses ExpressionInterpreter internally, which is NativeAOT compatible!
        var isMatch = activePremiumUsSpec.IsSatisfiedBy(testCustomer);
        Console.WriteLine($"Does testCustomer match activePremiumUsSpec? {isMatch}");

        // 2. Describe query for database execution
        var querySpec = activePremiumUsSpec.ToQuerySpec()
            .OrderByDescending(c => c.Id)
            .Take(10);

        // 3. Connect to DB (Requires docker-compose up)
        var connectionString = "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=postgres";

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            // Setup schema and seed data
            await SetupDatabaseAsync(connection).ConfigureAwait(false);

            Console.WriteLine("\n[2] Executing query via Dapper (AOT Safe):");

            // This translates the QuerySpec<T> to SQL via QuerySpecTranslator<T>
            // and executes it using Dapper.AOT source-generated methods.
            var translator = new QuerySpecTranslator<Customer>("Customers", VerbatimColumnNameResolver.Default);

            var results = await connection.QueryAsync(
                querySpec,
                translator,
                new PostgreSqlDialect()
            ).ConfigureAwait(false);

            foreach (var customer in results)
            {
                Console.WriteLine($"- Found: {customer.Name} (Tier: {customer.Tier}, Region: {customer.Region})");
            }
        }
        catch (NpgsqlException ex)
        {
            Console.WriteLine($"\n[!] Database connection failed. Make sure you run 'docker-compose up -d' first.");
            Console.WriteLine(ex.Message);
        }
    }

    internal static async Task SetupDatabaseAsync(DbConnection connection)
    {
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS ""Customers"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Name"" TEXT NOT NULL,
                ""IsActive"" BOOLEAN NOT NULL,
                ""Tier"" TEXT NOT NULL,
                ""Region"" TEXT NOT NULL
            );
            TRUNCATE TABLE ""Customers"";
        ").ConfigureAwait(false);

        await connection.ExecuteAsync(@"
            INSERT INTO ""Customers"" (""Name"", ""IsActive"", ""Tier"", ""Region"") VALUES
            ('Alice', true, 'Premium', 'US'),
            ('Bob', false, 'Premium', 'US'),
            ('Charlie', true, 'Standard', 'US'),
            ('Dave', true, 'Premium', 'EU'),
            ('Eve', true, 'Premium', 'US');
        ").ConfigureAwait(false);
    }
}




