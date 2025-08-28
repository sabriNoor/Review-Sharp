using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class DefaultSwitchCaseMissingService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            if (root == null)
            {
                return results;
            }

            var switchStatements = root.DescendantNodes().OfType<SwitchStatementSyntax>().ToList();
            foreach (var switchStmt in switchStatements)
            {
                bool hasDefault = switchStmt.Sections.Any(s => s.Labels.Any(l => l is DefaultSwitchLabelSyntax));
                if (!hasDefault)
                {
                    var lineNumber = switchStmt.SwitchKeyword.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "DefaultSwitchCaseMissing",
                        Severity = "Warning",
                        LineNumber = lineNumber,
                        Message = $"Switch statement at line {lineNumber} is missing a default case. Consider adding a default case to handle unexpected values."
                    });
                }
            }

            return results;
        }
    }
}
