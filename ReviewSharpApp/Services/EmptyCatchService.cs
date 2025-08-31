using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class EmptyCatchService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
          
            var catchClauses = root.DescendantNodes().OfType<CatchClauseSyntax>().ToList();
            
            foreach (var catchClause in catchClauses)
            {
                if (IsEmptyCatchBlock(catchClause))
                {
                    var lineNumber = catchClause.CatchKeyword.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    var exceptionType = catchClause.Declaration?.Type?.ToString() ?? "Exception";
                    
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "EmptyCatch",
                        Severity = "Warning",
                        LineNumber = lineNumber,
                        Message = $"Empty catch block for {exceptionType}. Consider logging the exception, rethrowing, or handling it appropriately. Empty catch blocks can hide bugs and make debugging difficult."
                    });
                }
            }

            return results;
        }

        private static bool IsEmptyCatchBlock(CatchClauseSyntax catchClause)
        {
            // Check if the catch block is empty or only contains comments
            if (catchClause.Block == null)
            {
                return true;
            }

            var statements = catchClause.Block.Statements;
            if (statements.Count == 0)
            {
                return true;
            }

            // Check if all statements are just comments or empty statements
            var nonEmptyStatements = statements.Where(s => !IsCommentOrEmptyStatement(s)).ToList();
            return nonEmptyStatements.Count == 0;
        }

        private static bool IsCommentOrEmptyStatement(StatementSyntax statement)
        {
            // Check for empty statements (just semicolon)
            if (statement is EmptyStatementSyntax)
            {
                return true;
            }

            // Check for expression statements that might be just comments
            if (statement is ExpressionStatementSyntax exprStmt)
            {
                var text = exprStmt.ToString().Trim();
                // If it's just whitespace or very short, it might be effectively empty
                return string.IsNullOrWhiteSpace(text) || text.Length <= 2;
            }

            return false;
        }
    }
}
