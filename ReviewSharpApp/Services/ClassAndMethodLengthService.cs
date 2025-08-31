using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class ClassAndMethodLengthService : ICodeReviewService
    {
        private const int DefaultMethodLineThreshold = 50;
        private const int DefaultClassLineThreshold = 300;

        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            // Check classes
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var classLines = GetLineCount(classDecl);
                if (classLines > DefaultClassLineThreshold)
                {
                    var line = classDecl.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "LengthCheck",
                        Severity = "Suggestion",
                        LineNumber = line,
                        Message = $"Class '{classDecl.Identifier.Text}' is {classLines} lines long. Consider splitting into smaller classes (threshold: {DefaultClassLineThreshold})."
                    });
                }
            }

            // Check methods
            foreach (var methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var methodLines = GetLineCount(methodDecl);
                if (methodLines > DefaultMethodLineThreshold)
                {
                    var line = methodDecl.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "LengthCheck",
                        Severity = "Suggestion",
                        LineNumber = line,
                        Message = $"Method '{methodDecl.Identifier.Text}' is {methodLines} lines long. Consider extracting smaller methods (threshold: {DefaultMethodLineThreshold})."
                    });
                }
            }

            return results;
        }

        private static int GetLineCount(SyntaxNode node)
        {
            var span = node.GetLocation().GetLineSpan();
            var start = span.StartLinePosition.Line;
            var end = span.EndLinePosition.Line;
            return (end - start + 1);
        }
    }
}
