// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.Specification;

namespace EricksonLopez.Specification.AotSmokeTest;

internal static class Program
{
    private static int _passedTests;

    private static void Assert([DoesNotReturnIf(false)] bool condition, string testName)
    {
        if (!condition)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {testName}");
            Console.ResetColor();
            Environment.Exit(1);
        }
        _passedTests++;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[PASS] {testName}");
        Console.ResetColor();
    }

    public static void Main()
    {
        Console.WriteLine("=================================================");
        Console.WriteLine(" EricksonLopez.Specification NativeAOT Suite     ");
        Console.WriteLine("=================================================");

        // ── 1. Concrete Specifications ────────────────────────────────────────────
        Console.WriteLine("\n--- 1. In-Memory Evaluation ---");

        var activeSpec = new ActiveUserSpec();
        var adultSpec = new AdultUserSpec(18);

        var user1 = new UserDto { Name = "Alice", IsActive = true, Age = 25 };
        var user2 = new UserDto { Name = "Bob", IsActive = false, Age = 30 };
        var user3 = new UserDto { Name = "Charlie", IsActive = true, Age = 15 };

        Assert(activeSpec.IsSatisfiedBy(user1), "ActiveUserSpec matches active user");
        Assert(!activeSpec.IsSatisfiedBy(user2), "ActiveUserSpec rejects inactive user");
        Assert(adultSpec.IsSatisfiedBy(user1), "AdultUserSpec matches adult user");
        Assert(!adultSpec.IsSatisfiedBy(user3), "AdultUserSpec rejects underage user");

        // ── 2. Composite Specifications (And, Or, Not) ────────────────────────────
        Console.WriteLine("\n--- 2. Composite Specification Combinators ---");

        var activeAdultSpec = activeSpec.And(adultSpec);
        Assert(activeAdultSpec.IsSatisfiedBy(user1), "activeAdultSpec matches active adult");
        Assert(!activeAdultSpec.IsSatisfiedBy(user2), "activeAdultSpec rejects inactive adult");
        Assert(!activeAdultSpec.IsSatisfiedBy(user3), "activeAdultSpec rejects active child");

        var activeOrAdultSpec = activeSpec.Or(adultSpec);
        Assert(activeOrAdultSpec.IsSatisfiedBy(user2), "activeOrAdultSpec matches inactive adult (OR)");
        Assert(activeOrAdultSpec.IsSatisfiedBy(user3), "activeOrAdultSpec matches active child (OR)");

        var notActiveSpec = activeSpec.Not();
        Assert(notActiveSpec.IsSatisfiedBy(user2), "notActiveSpec matches inactive user (NOT)");
        Assert(!notActiveSpec.IsSatisfiedBy(user1), "notActiveSpec rejects active user (NOT)");

        Console.WriteLine("\n=================================================");
        Console.WriteLine($" ALL {_passedTests} NATIVE AOT SUITE TESTS PASSED SUCCESSFULLY! ");
        Console.WriteLine("=== AOT Validator: OK ===");
        Console.WriteLine("=================================================");
    }
}
