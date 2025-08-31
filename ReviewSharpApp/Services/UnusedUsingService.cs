using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Linq;

namespace ReviewSharp.Services
{
    public class UnusedUsingService : ICodeReviewSemanticService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root, SemanticModel semanticModel)
        {
            var results = new List<CodeReviewResult>();

            // Iterate over all using directives
            foreach (var usingDirective in root.Usings)
            {
                if (usingDirective.StaticKeyword != default || usingDirective.Alias != null)
                    continue;
                var name = usingDirective.Name?.ToString();

                // Check if the namespace is referenced anywhere in the code
                bool isUsed = root.DescendantNodes()
                    .Where(node =>
                        node is IdentifierNameSyntax ||
                        node is QualifiedNameSyntax ||
                        node is GenericNameSyntax ||
                        node is ObjectCreationExpressionSyntax ||
                        node is AttributeSyntax)
                    .Any(node =>
                    {
                        var symbol = semanticModel.GetSymbolInfo(node).Symbol;
                        if (symbol is ITypeSymbol typeSymbol)
                        {
                            var ns = typeSymbol.ContainingNamespace?.ToDisplayString();
                            return ns != null && (ns == name || ns.StartsWith(name + "."));
                        }
                        var ns2 = symbol?.ContainingNamespace?.ToDisplayString();
                        return ns2 != null && (ns2 == name || ns2.StartsWith(name + "."));
                    });
                if (!isUsed)
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Unused Using Directive",
                        Message = $"Using directive '{name}' is not used.",
                        Severity = "Info",
                        LineNumber = usingDirective.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }

            return results;
        }
    }
}
