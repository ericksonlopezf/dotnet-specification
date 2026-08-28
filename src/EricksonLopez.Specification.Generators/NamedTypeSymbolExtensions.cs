// Copyright © Erickson Lopez. MIT License.
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EricksonLopez.Specification.Generators;

internal static class NamedTypeSymbolExtensions
{
    internal static bool IsPartial(this INamedTypeSymbol symbol)
    {
        // Stryker disable once Linq : In valid C# Roslyn syntax tree all partial class declarations have the partial modifier
        return symbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .Any(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
    }
}
