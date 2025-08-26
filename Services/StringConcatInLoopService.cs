using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class StringConcatInLoopService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            if (root == null)
            {
                return results;
            }

            var loopStatements = root.DescendantNodes().Where(n =>
                n is ForStatementSyntax ||
                n is ForEachStatementSyntax ||
                n is WhileStatementSyntax ||
                n is DoStatementSyntax).ToList();

            foreach (var loop in loopStatements)
            {
                var loopBody = GetLoopBody(loop);
                if (loopBody == null)
                {
                    continue;
                }

                var assignments = loopBody.DescendantNodes().OfType<AssignmentExpressionSyntax>().ToList();
                foreach (var assignment in assignments)
                {
                    if (IsStringConcatenationPattern(assignment, loopBody))
                    {
                        var line = assignment.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "StringConcatInLoop",
                            Severity = "Suggestion",
                            LineNumber = line,
                            Message = "String concatenation inside loops can be expensive. Consider using StringBuilder instead."
                        });
                    }
                }
            }

            return results;
        }

        private static StatementSyntax? GetLoopBody(SyntaxNode loop)
        {
            switch (loop)
            {
                case ForStatementSyntax f: return f.Statement;
                case ForEachStatementSyntax fe: return fe.Statement;
                case WhileStatementSyntax w: return w.Statement;
                case DoStatementSyntax d: return d.Statement;
                default: return null;
            }
        }

        private static bool IsStringConcatenationPattern(AssignmentExpressionSyntax assignment, SyntaxNode loopBody)
        {
            // Pattern 1: x += something; with x likely a string
            if (assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            {
                if (assignment.Left is IdentifierNameSyntax id)
                {
                    return IsLikelyStringVariable(id.Identifier.Text, loopBody);
                }
            }

            // Pattern 2: x = x + something; or x = something + x; where x likely string
            if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                if (assignment.Left is IdentifierNameSyntax leftId && assignment.Right is BinaryExpressionSyntax bin && bin.IsKind(SyntaxKind.AddExpression))
                {
                    var involvesSame =
                        (bin.Left is IdentifierNameSyntax l && l.Identifier.Text == leftId.Identifier.Text) ||
                        (bin.Right is IdentifierNameSyntax r && r.Identifier.Text == leftId.Identifier.Text);
                    if (involvesSame)
                    {
                        return IsLikelyStringVariable(leftId.Identifier.Text, loopBody);
                    }
                }
            }

            return false;
        }

        private static bool IsLikelyStringVariable(string name, SyntaxNode scope)
        {
            // Look for a local declaration like: string name = ...;
            var localDecls = scope.AncestorsAndSelf()
                .OfType<BlockSyntax>()
                .SelectMany(b => b.Statements.OfType<LocalDeclarationStatementSyntax>())
                .SelectMany(ld => ld.Declaration.Variables.Select(v => (ld.Declaration.Type, v)))
                .ToList();

            foreach (var (typeSyntax, variable) in localDecls)
            {
                if (variable.Identifier.Text != name)
                {
                    continue;
                }

                // string name; or System.String name;
                if (typeSyntax is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.StringKeyword))
                {
                    return true;
                }
                if (typeSyntax is IdentifierNameSyntax ins && ins.Identifier.Text == nameof(System.String))
                {
                    return true;
                }
                if (typeSyntax is QualifiedNameSyntax qns && qns.ToString() == "System.String")
                {
                    return true;
                }

                // var name = "" or other string-like initializer
                if (typeSyntax is IdentifierNameSyntax { Identifier.Text: "var" })
                {
                    var initializerValue = variable.Initializer?.Value;
                    if (initializerValue is LiteralExpressionSyntax les && les.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        return true;
                    }
                    if (initializerValue is InterpolatedStringExpressionSyntax)
                    {
                        return true;
                    }
                    if (initializerValue is MemberAccessExpressionSyntax maes && maes.ToString() == "string.Empty")
                    {
                        return true;
                    }
                }
            }

            // Fallback heuristic: if the variable is assigned a literal string within scope
            var stringLiteralAssignments = scope.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .Any(a => a.Left is IdentifierNameSyntax lid && lid.Identifier.Text == name && a.Right is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression));
            if (stringLiteralAssignments)
            {
                return true;
            }

            // Also consider if the identifier is used with string-specific members (e.g., .Length doesn't prove string)
            return false;
        }
    }
}
