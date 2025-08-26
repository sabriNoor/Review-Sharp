using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class SwitchStatementService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            if (root == null)
            {
                return results;
            }

            var ifStatements = root.DescendantNodes().OfType<IfStatementSyntax>().ToList();
            foreach (var ifStatement in ifStatements)
            {
                var chain = AnalyzeIfElseChain(ifStatement);
                if (chain != null && chain.ElseIfCount >= 3 && chain.SameVariableUsed)
                {
                    var lineNumber = ifStatement.IfKeyword.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "SwitchStatement",
                        Severity = "Suggestion",
                        LineNumber = lineNumber,
                        Message = $"Consider converting this if-else chain ({chain.ElseIfCount + 1} conditions) to a switch statement for better readability and performance."
                    });
                }
            }

            return results;
        }

        private static IfElseChainInfo? AnalyzeIfElseChain(IfStatementSyntax ifStatement)
        {
            var elseIfCount = 0;
            var variableNames = new HashSet<string>();
            var currentIf = ifStatement;

            // Analyze the initial if condition
            var initialCondition = ExtractVariableFromCondition(currentIf.Condition);
            if (initialCondition != null)
            {
                variableNames.Add(initialCondition);
            }

            // Count else-if blocks and collect variable names
            while (currentIf.Else?.Statement is IfStatementSyntax elseIf)
            {
                elseIfCount++;
                var condition = ExtractVariableFromCondition(elseIf.Condition);
                if (condition != null)
                {
                    variableNames.Add(condition);
                }
                currentIf = elseIf;
            }

            // Check if all conditions use the same variable
            var sameVariableUsed = variableNames.Count == 1;

            return new IfElseChainInfo
            {
                ElseIfCount = elseIfCount,
                SameVariableUsed = sameVariableUsed,
                VariableName = variableNames.FirstOrDefault()
            };
        }

        private static string? ExtractVariableFromCondition(ExpressionSyntax condition)
        {
            // Handle simple identifier expressions (e.g., "x == 5")
            if (condition is BinaryExpressionSyntax binaryExpr)
            {
                if (binaryExpr.Left is IdentifierNameSyntax identifier)
                {
                    return identifier.Identifier.Text;
                }
                if (binaryExpr.Right is IdentifierNameSyntax rightIdentifier)
                {
                    return rightIdentifier.Identifier.Text;
                }
            }

            // Handle method calls (e.g., "x.Equals(y)")
            if (condition is InvocationExpressionSyntax invocation)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    if (memberAccess.Expression is IdentifierNameSyntax identifier)
                    {
                        return identifier.Identifier.Text;
                    }
                }
            }

            // Handle simple identifier (e.g., "x" in "if (x)")
            if (condition is IdentifierNameSyntax simpleIdentifier)
            {
                return simpleIdentifier.Identifier.Text;
            }

            return null;
        }

        private class IfElseChainInfo
        {
            public int ElseIfCount { get; set; }
            public bool SameVariableUsed { get; set; }
            public string? VariableName { get; set; }
        }
    }
}
