using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class PossibleNullReferenceService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            if (root == null)
                return results;

            var allNullableNames = CollectNullableNames(root);
            var checkedNames = CollectNullCheckedNames(root);

            var memberAccesses = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToList();
            results.AddRange(CheckMemberAccessesForNull(memberAccesses, allNullableNames, checkedNames));

            // Check for nullable variables used in binary expressions, assignments, and returns
            var binaryExpressions = root.DescendantNodes().OfType<BinaryExpressionSyntax>();
            results.AddRange(CheckNullableUsageInExpressions(binaryExpressions, allNullableNames, checkedNames));

            var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();
            results.AddRange(CheckNullableUsageInAssignments(assignments, allNullableNames, checkedNames));

            var returns = root.DescendantNodes().OfType<ReturnStatementSyntax>();
            results.AddRange(CheckNullableUsageInReturns(returns, allNullableNames, checkedNames));

            return results;
        }
        private static HashSet<string> CollectNullCheckedNames(CompilationUnitSyntax root)
        {
            var checkedNames = new HashSet<string>();
            var ifStatements = root.DescendantNodes().OfType<IfStatementSyntax>();
            foreach (var ifStmt in ifStatements)
            {
                // if (p != null) or if (p is not null)
                if (ifStmt.Condition is BinaryExpressionSyntax binaryExpr)
                {
                    if (binaryExpr.Kind() == SyntaxKind.NotEqualsExpression)
                    {
                        if (binaryExpr.Left is IdentifierNameSyntax id)
                            checkedNames.Add(id.Identifier.Text);
                    }
                }
                if (ifStmt.Condition is IsPatternExpressionSyntax isPattern)
                {
                    if (isPattern.Expression is IdentifierNameSyntax id && isPattern.Pattern is UnaryPatternSyntax unary && unary.Pattern is ConstantPatternSyntax constPattern)
                    {
                        if (constPattern.Expression is LiteralExpressionSyntax lit && lit.Token.ValueText == "null")
                            checkedNames.Add(id.Identifier.Text);
                    }
                }
            }
            return checkedNames;
        }
        private static IEnumerable<CodeReviewResult> CheckNullableUsageInExpressions(IEnumerable<BinaryExpressionSyntax> expressions, HashSet<string> nullableNames, HashSet<string> checkedNames)
        {
            foreach (var expr in expressions)
            {
                foreach (var side in new[] { expr.Left, expr.Right })
                {
                    if (side is IdentifierNameSyntax id && nullableNames.Contains(id.Identifier.Text) && !checkedNames.Contains(id.Identifier.Text))
                    {
                        var lineNumber = expr.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        yield return new CodeReviewResult
                        {
                            RuleName = "PossibleNullReference",
                            Severity = "Warning",
                            LineNumber = lineNumber,
                            Message = $"Possible null reference on '{id.Identifier.Text}' at line {lineNumber}. Consider adding a null check."
                        };
                    }
                }
            }
        }

        private static IEnumerable<CodeReviewResult> CheckNullableUsageInAssignments(IEnumerable<AssignmentExpressionSyntax> assignments, HashSet<string> nullableNames, HashSet<string> checkedNames)
        {
            foreach (var assign in assignments)
            {
                if (assign.Right is IdentifierNameSyntax id && nullableNames.Contains(id.Identifier.Text) && !checkedNames.Contains(id.Identifier.Text))
                {
                    var lineNumber = assign.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    yield return new CodeReviewResult
                    {
                        RuleName = "PossibleNullReference",
                        Severity = "Warning",
                        LineNumber = lineNumber,
                        Message = $"Possible null reference on '{id.Identifier.Text}' at line {lineNumber}. Consider adding a null check."
                    };
                }
            }
        }

        private static IEnumerable<CodeReviewResult> CheckNullableUsageInReturns(IEnumerable<ReturnStatementSyntax> returns, HashSet<string> nullableNames, HashSet<string> checkedNames)
        {
            foreach (var ret in returns)
            {
                if (ret.Expression is IdentifierNameSyntax id && nullableNames.Contains(id.Identifier.Text) && !checkedNames.Contains(id.Identifier.Text))
                {
                    var lineNumber = ret.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    yield return new CodeReviewResult
                    {
                        RuleName = "PossibleNullReference",
                        Severity = "Warning",
                        LineNumber = lineNumber,
                        Message = $"Possible null reference on '{id.Identifier.Text}' at line {lineNumber}. Consider adding a null check."
                    };
                }
            }
        }

        private static HashSet<string> CollectNullableNames(CompilationUnitSyntax root)
        {
            var nullableLocals = root.DescendantNodes()
                .OfType<VariableDeclarationSyntax>()
                .Where(decl => decl.Type is NullableTypeSyntax)
                .SelectMany(decl => decl.Variables.Select(v => v.Identifier.Text))
                .ToHashSet();

            var nullableFields = root.DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .Where(decl => decl.Declaration.Type is NullableTypeSyntax)
                .SelectMany(decl => decl.Declaration.Variables.Select(v => v.Identifier.Text))
                .ToHashSet();

            var nullableProperties = root.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(decl => decl.Type is NullableTypeSyntax)
                .Select(decl => decl.Identifier.Text)
                .ToHashSet();

            var nullableParameters = root.DescendantNodes()
                .OfType<ParameterSyntax>()
                .Where(param => param.Type is NullableTypeSyntax)
                .Select(param => param.Identifier.Text)
                .ToHashSet();

            return new HashSet<string>(nullableLocals
                .Concat(nullableFields)
                .Concat(nullableProperties)
                .Concat(nullableParameters));
        }

        private static IEnumerable<CodeReviewResult> CheckMemberAccessesForNull(IEnumerable<MemberAccessExpressionSyntax> memberAccesses, HashSet<string> nullableNames, HashSet<string> checkedNames)
        {
            foreach (var memberAccess in memberAccesses)
            {
                if (memberAccess.Expression is IdentifierNameSyntax id)
                {
                    if (nullableNames.Contains(id.Identifier.Text) && !checkedNames.Contains(id.Identifier.Text))
                    {
                        var lineNumber = memberAccess.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        yield return new CodeReviewResult
                        {
                            RuleName = "PossibleNullReference",
                            Severity = "Warning",
                            LineNumber = lineNumber,
                            Message = $"Possible null reference on '{id.Identifier.Text}' at line {lineNumber}. Consider adding a null check."
                        };
                    }
                }
            }
        }
    }
}
