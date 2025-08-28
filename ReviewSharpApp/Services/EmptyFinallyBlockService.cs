using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class EmptyFinallyBlockService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            if (root == null)
            {
                return results;
            }

            var finallyClauses = root.DescendantNodes().OfType<FinallyClauseSyntax>().ToList();
            foreach (var finallyClause in finallyClauses)
            {
                if (IsEmptyFinallyBlock(finallyClause))
                {
                    var lineNumber = finallyClause.FinallyKeyword.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "EmptyFinallyBlock",
                        Severity = "Warning",
                        LineNumber = lineNumber,
                        Message = $"Empty finally block detected. Consider removing it or adding necessary cleanup code."
                    });
                }
            }

            return results;
        }

        private static bool IsEmptyFinallyBlock(FinallyClauseSyntax finallyClause)
        {
            if (finallyClause.Block == null)
            {
                return true;
            }

            var statements = finallyClause.Block.Statements;
            return statements.Count == 0;
        }
    }
}
