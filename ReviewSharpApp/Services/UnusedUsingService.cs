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
                if(usingDirective.StaticKeyword != default || usingDirective.Alias != null)
                    continue;
                var name = usingDirective.Name?.ToString();

                // Check if the namespace is referenced anywhere in the code
                bool isUsed = root.DescendantNodes()
                                  .OfType<IdentifierNameSyntax>()
                                  .Any(id => semanticModel.GetSymbolInfo(id).Symbol?.ContainingNamespace?.ToDisplayString() == name);

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
