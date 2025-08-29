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

            // Combine assignments and variable declarations
            var nodes = root.DescendantNodes()
                .Where(n => n is AssignmentExpressionSyntax || n is VariableDeclaratorSyntax);

            results.AddRange(CheckBoxing(nodes, semanticModel));
            results.AddRange(CheckUnboxing(root, semanticModel));

            return results;
        }

        private IEnumerable<CodeReviewResult> CheckBoxing(IEnumerable<SyntaxNode> nodes, SemanticModel semanticModel)
        {
            var results = new List<CodeReviewResult>();

            foreach (var node in nodes)
            {
                var (expr, targetType) = GetExpressionAndTargetType(node, semanticModel);

                if (expr == null || targetType == null) 
                    continue;

                var exprType = semanticModel.GetTypeInfo(expr).Type;
                if (exprType == null) 
                    continue;

                // Handle nullable value types safely
                var underlyingType = exprType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                    ? ((INamedTypeSymbol)exprType).TypeArguments[0]
                    : exprType;

                if (underlyingType.IsValueType &&
                    (targetType.SpecialType == SpecialType.System_Object || targetType.TypeKind == TypeKind.Interface))
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

        private (ExpressionSyntax? expr, ITypeSymbol? targetType) GetExpressionAndTargetType(SyntaxNode node, SemanticModel semanticModel)
        {
            return node switch
            {
                AssignmentExpressionSyntax a => (a.Right, semanticModel.GetTypeInfo(a.Left).Type),
                VariableDeclaratorSyntax v when v.Parent is VariableDeclarationSyntax decl => 
                    (v.Initializer?.Value, semanticModel.GetTypeInfo(decl.Type).Type),
                _ => (null, null)
            };
        }

        private IEnumerable<CodeReviewResult> CheckUnboxing(CompilationUnitSyntax root, SemanticModel semanticModel)
        {
            var results = new List<CodeReviewResult>();
            var casts = root.DescendantNodes().OfType<CastExpressionSyntax>();

            foreach (var castExpr in casts)
            {
                var castType = semanticModel.GetTypeInfo(castExpr.Type).Type;
                var innerExprType = semanticModel.GetTypeInfo(castExpr.Expression).Type;

                if (castType != null && innerExprType != null)
                {
                    // Handle nullable value types safely
                    var targetType = castType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                        ? ((INamedTypeSymbol)castType).TypeArguments[0]
                        : castType;

                    if (targetType.IsValueType && innerExprType.SpecialType == SpecialType.System_Object)
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
            }

            return results;
        }
    }
}
