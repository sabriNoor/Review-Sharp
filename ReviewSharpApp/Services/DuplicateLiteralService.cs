using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class DuplicateLiteralService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            if (root == null) return results;

            var literalGroups = root.DescendantNodes()
                .OfType<LiteralExpressionSyntax>()
                .GroupBy(l => l.Token.ValueText)
                .Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key));

            foreach (var group in literalGroups)
            {
                foreach (var literal in group)
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "DuplicateLiteral",
                        Message = $"Duplicate literal '{group.Key}' detected. Consider defining a constant.",
                        Severity = "Warning",
                        LineNumber = literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }
    }
}
