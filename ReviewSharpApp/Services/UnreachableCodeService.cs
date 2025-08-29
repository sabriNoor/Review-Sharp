using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Linq;

namespace ReviewSharp.Services
{
    public class UnreachableCodeService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();

            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                results.AddRange(CheckUnreachableCode(method));
            }

            return results;
        }

        private IEnumerable<CodeReviewResult> CheckUnreachableCode(MethodDeclarationSyntax method)
        {
            var results = new List<CodeReviewResult>();

            // Walk through all blocks in the method
            var blocks = method.DescendantNodes().OfType<BlockSyntax>();
            foreach (var block in blocks)
            {
                bool foundTerminator = false;

                foreach (var statement in block.Statements)
                {
                    if (foundTerminator)
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "Unreachable Code",
                            Message = $"Unreachable code detected after a terminating statement in method '{method.Identifier.Text}'.",
                            Severity = "Warning",
                            LineNumber = statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        });
                        // continue scanning to flag multiple unreachable statements
                    }

                    if (IsTerminatingStatement(statement))
                    {
                        foundTerminator = true;
                    }
                }
            }

            return results;
        }

        private bool IsTerminatingStatement(StatementSyntax statement)
        {
            return statement is ReturnStatementSyntax
                || statement is ThrowStatementSyntax
                || statement is BreakStatementSyntax
                || statement is ContinueStatementSyntax;
        }
    }
}
