using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class UnusedSymbolService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            if (root == null)
            {
                return results;
            }

            AnalyzeUnusedPrivateFields(root, results);
            AnalyzeUnusedMethodSymbols(root, results);
            AnalyzeUnusedLocalFunctionParameters(root, results);

            return results;
        }

        private static void AnalyzeUnusedPrivateFields(CompilationUnitSyntax root, List<CodeReviewResult> results)
        {
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var privateFields = classDecl.Members
                    .OfType<FieldDeclarationSyntax>()
                    .Where(f => f.Modifiers.Any(m => m.Text == "private"))
                    .SelectMany(f => f.Declaration.Variables.Select(v => (field: f, variable: v)))
                    .ToList();

                if (privateFields.Count == 0)
                {
                    continue;
                }

                var classIdentifierUsages = classDecl.DescendantNodes().OfType<IdentifierNameSyntax>().ToList();

                foreach (var (field, variable) in privateFields)
                {
                    var name = variable.Identifier.Text;
                    var declLocation = variable.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    var usageCount = classIdentifierUsages.Count(id => id.Identifier.Text == name && id.Parent != variable.Parent);
                    if (usageCount == 0)
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "UnusedSymbol",
                            Severity = "Warning",
                            LineNumber = declLocation,
                            Message = $"Private field '{name}' is declared but never used. Consider removing it."
                        });
                    }
                }
            }
        }

        private static void AnalyzeUnusedMethodSymbols(CompilationUnitSyntax root, List<CodeReviewResult> results)
        {
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var bodyNode = method.Body as SyntaxNode ?? method.ExpressionBody as SyntaxNode;
                if (bodyNode == null)
                {
                    continue;
                }

                var identifierUsages = bodyNode.DescendantNodes().OfType<IdentifierNameSyntax>().ToList();

                foreach (var parameter in method.ParameterList?.Parameters ?? new SeparatedSyntaxList<ParameterSyntax>())
                {
                    var name = parameter.Identifier.Text;
                    var declLocation = parameter.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    var used = identifierUsages.Any(id => id.Identifier.Text == name);
                    if (!used)
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "UnusedSymbol",
                            Severity = "Warning",
                            LineNumber = declLocation,
                            Message = $"Parameter '{name}' is never used within the method. Consider removing it or prefix with '_' to indicate intentional unused."
                        });
                    }
                }

                foreach (var local in bodyNode.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
                {
                    foreach (var variable in local.Declaration.Variables)
                    {
                        var name = variable.Identifier.Text;
                        var declLocation = variable.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        var used = identifierUsages.Any(id => id.Identifier.Text == name);
                        if (!used)
                        {
                            results.Add(new CodeReviewResult
                            {
                                RuleName = "UnusedSymbol",
                                Severity = "Warning",
                                LineNumber = declLocation,
                                Message = $"Local variable '{name}' is declared but never used. Consider removing it."
                            });
                        }
                    }
                }
            }
        }

        private static void AnalyzeUnusedLocalFunctionParameters(CompilationUnitSyntax root, List<CodeReviewResult> results)
        {
            foreach (var localFunc in root.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
            {
                var bodyNode = localFunc.Body as SyntaxNode ?? localFunc.ExpressionBody as SyntaxNode;
                if (bodyNode == null)
                {
                    continue;
                }

                var identifierUsages = bodyNode.DescendantNodes().OfType<IdentifierNameSyntax>().ToList();
                foreach (var parameter in localFunc.ParameterList?.Parameters ?? new SeparatedSyntaxList<ParameterSyntax>())
                {
                    var name = parameter.Identifier.Text;
                    var declLocation = parameter.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    var used = identifierUsages.Any(id => id.Identifier.Text == name);
                    if (!used)
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "UnusedSymbol",
                            Severity = "Warning",
                            LineNumber = declLocation,
                            Message = $"Parameter '{name}' is never used within the local function. Consider removing it or prefix with '_' to indicate intentional unused."
                        });
                    }
                }
            }
        }
    }
}


