using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Models;
using ReviewSharp.Interfaces;

namespace ReviewSharp.Services
{
    public class ManyMethodParametersService : ICodeReviewService
    {
        private const int ParameterThreshold = 7; // You can adjust this threshold

        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                int paramCount = method.ParameterList.Parameters.Count;
                if (paramCount > ParameterThreshold)
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "ManyMethodParameters",
                        Severity = "Warning",
                        LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Message = $"Method '{method.Identifier.Text}' has {paramCount} parameters. Consider refactoring to reduce parameter count."
                    });
                }
            }
            return results;
        }
    }
}
