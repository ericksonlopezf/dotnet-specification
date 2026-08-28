// Copyright © Erickson Lopez. MIT License.
using System;
using Microsoft.CodeAnalysis;

namespace EricksonLopez.Specification.Analyzers;

internal static class AnalyzerExtensions
{
    public static bool InheritsFromSpecification(this INamedTypeSymbol type)
    {
        var baseType = type.BaseType;
        while (baseType is not null)
        {
            if (baseType.Name == "Specification" &&
                baseType.TypeArguments.Length == 1 &&
                baseType.ContainingNamespace!.ToString() == "EricksonLopez.Specification")
            {
                return true;
            }

            baseType = baseType.BaseType;
        }
        return false;
    }
}


