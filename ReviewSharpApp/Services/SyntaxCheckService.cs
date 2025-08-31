using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class SyntaxCheckService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();

            var syntaxTree = root.SyntaxTree;
            if (syntaxTree == null)
            {
                return results;
            }

            foreach (var diagnostic in syntaxTree.GetDiagnostics())
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error)
                {
                    continue;
                }

                var lineSpan = diagnostic.Location.GetLineSpan();
                var lineNumber = lineSpan.StartLinePosition.Line + 1; // 1-based

                results.Add(new CodeReviewResult
                {
                    RuleName = "SyntaxCheck",
                    Message = diagnostic.GetMessage(),
                    Severity = "Error",
                    LineNumber = lineNumber
                });
            }

            return results;
        }
    }
}


