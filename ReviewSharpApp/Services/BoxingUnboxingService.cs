using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Linq;

namespace ReviewSharp.Services
{
    public class BoxingUnboxingService : ICodeReviewSemanticService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root, SemanticModel semanticModel)
        {
            var results = new List<CodeReviewResult>();

            var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();
            var declarations = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();

            results.AddRange(CheckBoxing(assignments, semanticModel));
            results.AddRange(CheckBoxing(declarations, semanticModel));

            results.AddRange(CheckUnboxing(root, semanticModel));

            return results;
        }

        private IEnumerable<CodeReviewResult> CheckBoxing(
            IEnumerable<SyntaxNode> nodes,
            SemanticModel semanticModel)
        {
            var results = new List<CodeReviewResult>();

            foreach (var node in nodes)
            {
                ExpressionSyntax? expr = node switch
                {
                    AssignmentExpressionSyntax a => a.Right,
                    VariableDeclaratorSyntax v => v.Initializer?.Value,
                    _ => null
                };

                if (expr == null)
                    continue;

                var exprType = semanticModel.GetTypeInfo(expr).Type;
                if (exprType == null)
                    continue;

                ITypeSymbol? targetType = node switch
                {
                    AssignmentExpressionSyntax a => semanticModel.GetTypeInfo(a.Left).Type,
                    VariableDeclaratorSyntax v when v.Parent is VariableDeclarationSyntax decl =>
                        semanticModel.GetTypeInfo(decl.Type).Type,
                    _ => null
                };

                if (targetType == null)
                    continue;

                if (exprType.IsValueType && (targetType.SpecialType == SpecialType.System_Object || targetType.TypeKind == TypeKind.Interface))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Unnecessary Boxing",
                        Message = $"Value type '{exprType}' assigned to '{targetType}' may cause unnecessary boxing.",
                        Severity = "Warning",
                        LineNumber = expr.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }

            return results;
        }

        private IEnumerable<CodeReviewResult> CheckUnboxing(CompilationUnitSyntax root, SemanticModel semanticModel)
        {
            var results = new List<CodeReviewResult>();

            var casts = root.DescendantNodes().OfType<CastExpressionSyntax>();

            foreach (var castExpr in casts)
            {
                var castType = semanticModel.GetTypeInfo(castExpr.Type).Type;
                var innerExprType = semanticModel.GetTypeInfo(castExpr.Expression).Type;

                if (castType != null && innerExprType != null &&
                    castType.IsValueType && innerExprType.SpecialType == SpecialType.System_Object)
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Unnecessary Unboxing",
                        Message = $"Object '{innerExprType}' cast to value type '{castType}' may cause unnecessary unboxing.",
                        Severity = "Warning",
                        LineNumber = castExpr.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }

            return results;
        }
    }
}
