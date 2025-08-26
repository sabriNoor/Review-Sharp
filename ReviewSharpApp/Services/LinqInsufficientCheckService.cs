using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Linq;

namespace ReviewSharp.Services
{
    public class LinqInsufficientCheckService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                results.AddRange(CheckInsufficientPredicate(invocation));
                results.AddRange(CheckInefficientToListUsage(invocation));
                results.AddRange(CheckCountVsAny(invocation));
                results.AddRange(CheckChainedWhere(invocation));
                results.AddRange(CheckFirstOrDefaultNullHandling(invocation));
            }
            return results;
        }

        // 1. .First(), .Single(), .Last() without predicate
        private IEnumerable<CodeReviewResult> CheckInsufficientPredicate(InvocationExpressionSyntax invocation)
        {
            var results = new List<CodeReviewResult>();
            var exprStr = invocation.Expression.ToString();
            if ((exprStr.EndsWith(".First") || exprStr.EndsWith(".Single") || exprStr.EndsWith(".Last")) &&
                invocation.ArgumentList.Arguments.Count == 0)
            {
                results.Add(new CodeReviewResult
                {
                    RuleName = "LINQ Insufficient Check",
                    Message = $"LINQ method '{exprStr}' is used without a predicate. Consider using '{exprStr}(...predicate...)' or '{exprStr}OrDefault' to avoid exceptions if no match is found.",
                    Severity = "Warning",
                    LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                });
            }
            return results;
        }

        // 2. Inefficient .ToList()/.ToArray() before filtering/projecting
        private IEnumerable<CodeReviewResult> CheckInefficientToListUsage(InvocationExpressionSyntax invocation)
        {
            var results = new List<CodeReviewResult>();
            var exprStr = invocation.Expression.ToString();
            if (exprStr.EndsWith(".ToList") || exprStr.EndsWith(".ToArray"))
            {
                var parent = invocation.Parent as MemberAccessExpressionSyntax;
                if (parent != null && (parent.Name.Identifier.Text == "Where" || parent.Name.Identifier.Text == "Select"))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "LINQ Inefficient Materialization",
                        Message = $"Calling '{exprStr}' before filtering or projecting can be inefficient. Consider filtering/projecting first, then materializing.",
                        Severity = "Suggestion",
                        LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }

        // 3. .Count() > 0 instead of .Any()
        private IEnumerable<CodeReviewResult> CheckCountVsAny(InvocationExpressionSyntax invocation)
        {
            var results = new List<CodeReviewResult>();
            var exprStr = invocation.Expression.ToString();
            if (exprStr.EndsWith(".Count"))
            {
                var parent = invocation.Parent;
                if (parent is BinaryExpressionSyntax binary && binary.OperatorToken.Text == ">" && binary.Right.ToString() == "0")
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "LINQ Existence Check",
                        Message = $"Use '.Any()' instead of '.Count() > 0' for existence checks. '.Any()' is more efficient.",
                        Severity = "Suggestion",
                        LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }

        // 4. Chained .Where() calls
        private IEnumerable<CodeReviewResult> CheckChainedWhere(InvocationExpressionSyntax invocation)
        {
            var results = new List<CodeReviewResult>();
            var exprStr = invocation.Expression.ToString();
            if (exprStr.EndsWith(".Where"))
            {
                // Check for chained .Where() via MemberAccessExpressionSyntax only
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression is InvocationExpressionSyntax prevInvocation)
                {
                    var prevExprStr = prevInvocation.Expression.ToString();
                    if (prevExprStr.EndsWith(".Where"))
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "LINQ Chained Where",
                            Message = "Combine multiple .Where() calls into a single predicate for better readability and performance.",
                            Severity = "Suggestion",
                            LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        });
                    }
                }
            }
            return results;
        }

        // 5. .FirstOrDefault() without null handling
        private IEnumerable<CodeReviewResult> CheckFirstOrDefaultNullHandling(InvocationExpressionSyntax invocation)
        {
            var results = new List<CodeReviewResult>();
            var exprStr = invocation.Expression.ToString();
            if (exprStr.EndsWith(".FirstOrDefault"))
            {
                // Only warn for direct member access (e.g., .FirstOrDefault().Property)
                var parent = invocation.Parent;
                if (parent is MemberAccessExpressionSyntax)
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "LINQ Null Handling",
                        Message = "Result of '.FirstOrDefault()' should be checked for null before accessing members.",
                        Severity = "Warning",
                        LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }
    }
}
