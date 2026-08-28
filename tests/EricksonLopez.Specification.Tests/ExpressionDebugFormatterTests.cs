// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

public sealed class ExpressionDebugFormatterTests
{
    private sealed class Customer
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public decimal CreditLimit { get; set; }
        public string? Category { get; set; }
        public Address? Address { get; set; }

        public bool CalculateEligibility(int bonus, string code) => true;
        public bool Between(int lower, int upper) => Age >= lower && Age <= upper;
        public bool Between(int a, int b, int c) => true;
        public bool Contains(int a, int b) => true;
    }

    private sealed class Address
    {
        public string? City { get; set; }
        public bool Between(int lower, int upper) => true;
    }

    private class ClosureHolder
    {
        public int TargetAge = 30;
        public string TargetName { get; set; } = "Alice";
    }

    [Fact]
    public void Format_NullExpression_ThrowsArgumentNullException()
    {
        var act = () => ExpressionDebugFormatter.Format<Customer>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Format_BinaryOperators_FormatCorrectly()
    {
        // Equal
        Expression<Func<Customer, bool>> eq = c => c.Age == 25;
        ExpressionDebugFormatter.Format(eq).Should().Be("Age == 25");

        // NotEqual
        Expression<Func<Customer, bool>> ne = c => c.Age != 25;
        ExpressionDebugFormatter.Format(ne).Should().Be("Age != 25");

        // GreaterThan
        Expression<Func<Customer, bool>> gt = c => c.Age > 18;
        ExpressionDebugFormatter.Format(gt).Should().Be("Age > 18");

        // GreaterThanOrEqual
        Expression<Func<Customer, bool>> gte = c => c.Age >= 18;
        ExpressionDebugFormatter.Format(gte).Should().Be("Age >= 18");

        // LessThan
        Expression<Func<Customer, bool>> lt = c => c.Age < 65;
        ExpressionDebugFormatter.Format(lt).Should().Be("Age < 65");

        // LessThanOrEqual
        Expression<Func<Customer, bool>> lte = c => c.Age <= 65;
        ExpressionDebugFormatter.Format(lte).Should().Be("Age <= 65");

        // AndAlso
        Expression<Func<Customer, bool>> andAlso = c => c.Age >= 18 && c.IsActive;
        ExpressionDebugFormatter.Format(andAlso).Should().Be("(Age >= 18) AND (IsActive)");

        // OrElse
        Expression<Func<Customer, bool>> orElse = c => c.Age < 18 || c.IsActive;
        ExpressionDebugFormatter.Format(orElse).Should().Be("(Age < 18) OR (IsActive)");

        // Coalesce
        Expression<Func<Customer, bool>> coalesce = c => (c.Category ?? "Standard") == "VIP";
        ExpressionDebugFormatter.Format(coalesce).Should().Be("(Category ?? \"Standard\") == \"VIP\"");
    }

    [Fact]
    public void Format_UnaryOperators_FormatCorrectly()
    {
        // Not
        Expression<Func<Customer, bool>> not = c => !c.IsActive;
        ExpressionDebugFormatter.Format(not).Should().Be("NOT(IsActive)");

        // Convert (boxing / cast)
        var p = Expression.Parameter(typeof(Customer), "c");
        var prop = Expression.Property(p, nameof(Customer.Age));
        var convert = Expression.Convert(prop, typeof(object));
        var notNull = Expression.NotEqual(convert, Expression.Constant(null, typeof(object)));
        var lambda = Expression.Lambda<Func<Customer, bool>>(notNull, p);
        ExpressionDebugFormatter.Format(lambda).Should().Be("Age != null");
    }

    [Fact]
    public void Format_ConstantsAndClosures_FormatCorrectly()
    {
        // null constant
        Expression<Func<Customer, bool>> nullConst = c => c.Name == null;
        ExpressionDebugFormatter.Format(nullConst).Should().Be("Name == null");

        // string constant
        Expression<Func<Customer, bool>> stringConst = c => c.Name == "Bob";
        ExpressionDebugFormatter.Format(stringConst).Should().Be("Name == \"Bob\"");

        // boolean constants
        Expression<Func<Customer, bool>> boolTrue = c => c.IsActive == true;
        ExpressionDebugFormatter.Format(boolTrue).Should().Be("IsActive == true");

        Expression<Func<Customer, bool>> boolFalse = c => c.IsActive == false;
        ExpressionDebugFormatter.Format(boolFalse).Should().Be("IsActive == false");

        // Closure captured field (local variable captured in compiler display class)
        int targetAge = 30;
        Expression<Func<Customer, bool>> capturedField = c => c.Age == targetAge;
        ExpressionDebugFormatter.Format(capturedField).Should().Be("Age == 30");

        // Closure captured string field
        string targetName = "Alice";
        Expression<Func<Customer, bool>> capturedProp = c => c.Name == targetName;
        ExpressionDebugFormatter.Format(capturedProp).Should().Be("Name == \"Alice\"");


    }

    [Fact]
    public void Format_StringMethodCalls_FormatCorrectly()
    {
        // Contains
        Expression<Func<Customer, bool>> contains = c => c.Name!.Contains("Corp");
        ExpressionDebugFormatter.Format(contains).Should().Be("Name CONTAINS(\"Corp\")");

        // StartsWith
        Expression<Func<Customer, bool>> startsWith = c => c.Name!.StartsWith("A");
        ExpressionDebugFormatter.Format(startsWith).Should().Be("Name STARTS_WITH(\"A\")");

        // EndsWith
        Expression<Func<Customer, bool>> endsWith = c => c.Name!.EndsWith("Inc");
        ExpressionDebugFormatter.Format(endsWith).Should().Be("Name ENDS_WITH(\"Inc\")");

        // Other string instance method
        Expression<Func<Customer, bool>> indexOf = c => c.Name!.IndexOf("X") > 0;
        ExpressionDebugFormatter.Format(indexOf).Should().Be("Name IndexOf(\"X\") > 0");
    }

    [Fact]
    public void Format_CollectionContains_FormatsAsIn()
    {
        var ids = new List<int> { 1, 2, 3 };

        // Instance collection.Contains(c.Id)
        Expression<Func<Customer, bool>> instContains = c => ids.Contains(c.Id);
        ExpressionDebugFormatter.Format(instContains).Should().Be("Id IN (...)");

        // Enumerable.Contains(ids, c.Id) static extension method
        var p = Expression.Parameter(typeof(Customer), "c");
        var idProp = Expression.Property(p, nameof(Customer.Id));
        var staticContains = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(int));
        var staticCall = Expression.Call(null, staticContains, Expression.Constant(ids), idProp);
        var extContains = Expression.Lambda<Func<Customer, bool>>(staticCall, p);
        ExpressionDebugFormatter.Format(extContains).Should().Be("Id IN (...)");
    }


    [Fact]
    public void Format_BetweenMethod_FormatsAsBetween()
    {
        // Extension method named Between
        var p = Expression.Parameter(typeof(Customer), "c");
        var ageProp = Expression.Property(p, nameof(Customer.Age));
        var betweenMethod = typeof(ExpressionDebugFormatterTests).GetMethod(nameof(Between))!;
        var call = Expression.Call(null, betweenMethod, ageProp, Expression.Constant(18), Expression.Constant(65));
        var lambda = Expression.Lambda<Func<Customer, bool>>(call, p);

        var str = ExpressionDebugFormatter.Format(lambda);
        str.Should().Be("Age BETWEEN (18, 65)");
    }

    [Fact]
    public void Format_NestedMemberAccess_FormatsDirectMemberName()
    {
        Expression<Func<Customer, bool>> nested = c => c.Address!.City == "New York";
        ExpressionDebugFormatter.Format(nested).Should().Be("City == \"New York\"");
    }

    [Fact]
    public void Format_FallbackBinaryOperator_FormatsOperatorName()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var add = Expression.Add(Expression.Property(p, nameof(Customer.Age)), Expression.Constant(1));
        var eq = Expression.Equal(add, Expression.Constant(20));
        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, p);

        ExpressionDebugFormatter.Format(lambda).Should().Be("Age Add 1 == 20");
    }

    [Fact]
    public void Format_IExpressionSpecification_DefaultToDebugString_Works()
    {
        var customSpec = new CustomInterfaceSpec();
        ((IExpressionSpecification<Customer>)customSpec).ToDebugString().Should().Be("Age >= 18");
    }

    private sealed class CustomInterfaceSpec : IExpressionSpecification<Customer>
    {
        public bool IsSatisfiedBy(Customer entity) => entity.Age >= 18;
        public Expression<Func<Customer, bool>> ToExpression() => c => c.Age >= 18;
    }

    public static bool Between(int value, int lower, int upper) => value >= lower && value <= upper;

    [Fact]
    public void Format_InstanceBetween_FormatsAsBetween()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var addressProp = Expression.Property(p, nameof(Customer.Address));
        var instBetweenMethod = typeof(Address).GetMethod(nameof(Address.Between), new[] { typeof(int), typeof(int) })!;
        var call = Expression.Call(addressProp, instBetweenMethod, Expression.Constant(10), Expression.Constant(50));
        var lambda = Expression.Lambda<Func<Customer, bool>>(call, p);

        var str = ExpressionDebugFormatter.Format(lambda);
        str.Should().Be("Address BETWEEN (10, 50)");
    }

    [Fact]
    public void Format_NonMatchingBetween_FallsThroughToGeneric()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var between0Args = typeof(ExpressionDebugFormatterTests).GetMethod(nameof(BetweenNoArgs))!;
        var call0 = Expression.Call(null, between0Args);
        var lambda0 = Expression.Lambda<Func<Customer, bool>>(call0, p);
        ExpressionDebugFormatter.Format(lambda0).Should().Be("BetweenNoArgs()");
    }

    [Fact]
    public void Format_NonMatchingContains_FallsThroughToGeneric()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var contains0Args = typeof(ExpressionDebugFormatterTests).GetMethod(nameof(ContainsNoArgs))!;
        var call0 = Expression.Call(null, contains0Args);
        var lambda0 = Expression.Lambda<Func<Customer, bool>>(call0, p);
        ExpressionDebugFormatter.Format(lambda0).Should().Be("ContainsNoArgs()");
    }

    [Fact]
    public void Format_StringZeroArgMethod_FormatsWithoutArguments()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var nameProp = Expression.Property(p, nameof(Customer.Name));
        var trimMethod = typeof(string).GetMethod(nameof(string.Trim), Type.EmptyTypes)!;
        var call = Expression.Call(nameProp, trimMethod);
        var eq = Expression.Equal(call, Expression.Constant("Bob"));
        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, p);

        ExpressionDebugFormatter.Format(lambda).Should().Be("Name Trim() == \"Bob\"");
    }

    [Fact]
    public void Format_UnaryNegate_VisitsOperand()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var ageProp = Expression.Property(p, nameof(Customer.Age));
        var negate = Expression.Negate(ageProp);
        var eq = Expression.Equal(negate, Expression.Constant(-25));
        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, p);

        ExpressionDebugFormatter.Format(lambda).Should().Be("Age == -25");
    }

    [Fact]
    public void Format_ConstantProperty_FormatsValue()
    {
        var date = new DateTime(2025, 1, 1);
        var constDate = Expression.Constant(date);
        var yearProp = Expression.Property(constDate, nameof(DateTime.Year));
        var p = Expression.Parameter(typeof(Customer), "c");
        var eq = Expression.Equal(Expression.Property(p, nameof(Customer.Age)), yearProp);
        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, p);

        ExpressionDebugFormatter.Format(lambda).Should().Be("Age == 2025");
    }

    public static bool BetweenNoArgs() => true;
    public static bool ContainsNoArgs() => true;
    public static bool ContainsStatic1(int id) => true;
    public static bool BetweenStatic2(int age, int lower) => true;

    [Fact]
    public void Format_ContainsOverloads_FallbackWhenArgCountMismatch()
    {
        var p = Expression.Parameter(typeof(Customer), "c");

        // Static Contains with 1 argument
        var containsStatic1Method = typeof(ExpressionDebugFormatterTests).GetMethod(nameof(ContainsStatic1))!;
        var callStatic1 = Expression.Call(null, containsStatic1Method, Expression.Property(p, nameof(Customer.Id)));
        var lambdaStatic1 = Expression.Lambda<Func<Customer, bool>>(callStatic1, p);
        ExpressionDebugFormatter.Format(lambdaStatic1).Should().Be("ContainsStatic1(Id)");
    }

    [Fact]
    public void Format_BetweenOverloads_FallbackWhenArgCountMismatch()
    {
        var p = Expression.Parameter(typeof(Customer), "c");

        // Static Between with 2 arguments
        var betweenStatic2Method = typeof(ExpressionDebugFormatterTests).GetMethod(nameof(BetweenStatic2))!;
        var callStatic2 = Expression.Call(null, betweenStatic2Method, Expression.Property(p, nameof(Customer.Age)), Expression.Constant(18));
        var lambdaStatic2 = Expression.Lambda<Func<Customer, bool>>(callStatic2, p);
        ExpressionDebugFormatter.Format(lambdaStatic2).Should().Be("BetweenStatic2(Age, 18)");
    }

    [Fact]
    public void Format_ConditionalExpression_FormatsTernary()
    {
        Expression<Func<Customer, bool>> ternary = c => (c.IsActive ? c.Age > 18 : c.Age > 21);
        ExpressionDebugFormatter.Format(ternary).Should().Be("(IsActive ? Age > 18 : Age > 21)");
    }

    [Fact]
    public void Format_TypeBinaryExpression_FormatsIsType()
    {
        Expression<Func<Customer, bool>> typeIs = c => c.Address is Address;
        ExpressionDebugFormatter.Format(typeIs).Should().Be("Address IS Address");
    }

    [Fact]
    public void Format_CustomObjectValue_FormatsToStringOrNull()
    {
        var custom = new CustomToString("SpecialValue");
        var constCustom = Expression.Constant(custom, typeof(object));
        var p = Expression.Parameter(typeof(Customer), "c");
        var objProp = Expression.Convert(Expression.Property(p, nameof(Customer.Address)), typeof(object));
        var eqCustom = Expression.Equal(objProp, constCustom);
        var lambdaCustom = Expression.Lambda<Func<Customer, bool>>(eqCustom, p);
        ExpressionDebugFormatter.Format(lambdaCustom).Should().Be("Address == SpecialValue");

        var nullToString = new NullToStringClass();
        var constNullStr = Expression.Constant(nullToString, typeof(object));
        var eqNull = Expression.Equal(objProp, constNullStr);
        var lambdaNull = Expression.Lambda<Func<Customer, bool>>(eqNull, p);
        ExpressionDebugFormatter.Format(lambdaNull).Should().Be("Address == null");
    }

    private sealed class CustomToString(string val)
    {
        public override string ToString() => val;
    }

    private sealed class NullToStringClass
    {
        public override string? ToString() => null;
    }

    [Fact]
    public void Format_MethodCall_ContainsWithNullColumnExpr_FallsThrough()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var containsMethod = typeof(Customer).GetMethod(nameof(Customer.Contains), new[] { typeof(int), typeof(int) })!;
        var call = Expression.Call(p, containsMethod, Expression.Constant(1), Expression.Constant(2));
        var lambda = Expression.Lambda<Func<Customer, bool>>(call, p);

        ExpressionDebugFormatter.Format(lambda).Should().Be("Contains(1, 2)");
    }

    [Fact]
    public void Format_MethodCall_BetweenWith3Args_FallsThrough()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var between3Method = typeof(Customer).GetMethod(nameof(Customer.Between), new[] { typeof(int), typeof(int), typeof(int) })!;
        var call = Expression.Call(p, between3Method, Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));
        var lambda = Expression.Lambda<Func<Customer, bool>>(call, p);

        ExpressionDebugFormatter.Format(lambda).Should().Be("Between(1, 2, 3)");
    }

    [Fact]
    public void Format_DefaultInstance_Exists()
    {
        ExpressionDebugFormatter.Default.Should().NotBeNull();
    }

    [Fact]
    public void Format_ClosurePropertyCapture_FormatsExtractedValue()
    {
        var holder = new ClosureHolder { TargetName = "Bob" };
        var param = Expression.Parameter(typeof(Customer), "c");
        var propExpr = Expression.Property(Expression.Constant(holder), typeof(ClosureHolder).GetProperty(nameof(ClosureHolder.TargetName))!);
        var eq = Expression.Equal(Expression.Property(param, nameof(Customer.Name)), propExpr);
        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, param);

        ExpressionDebugFormatter.Format(lambda).Should().Be("Name == \"Bob\"");
    }
}




