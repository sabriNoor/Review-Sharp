using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Linq;

namespace ReviewSharp.Services
{
    public class NullCheckStyleService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var binaryExpressions = root.DescendantNodes().OfType<BinaryExpressionSyntax>();
            foreach (var expr in binaryExpressions)
            {
                if ((expr.OperatorToken.Text == "==" || expr.OperatorToken.Text == "!=") &&
                    (expr.Left.ToString() == "null" || expr.Right.ToString() == "null"))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Null Check Style",
                        Message = $"Use 'is null' or 'is not null' instead of '{expr.OperatorToken.Text} null'.",
                        Severity = "Suggestion",
                        LineNumber = expr.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }
    }
}
