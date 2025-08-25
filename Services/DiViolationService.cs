using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Linq;

namespace ReviewSharp.Services
{
    public class DiViolationService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            results.AddRange(CheckDirectInstantiation(root));
            results.AddRange(CheckMissingConstructorInjection(root));
            results.AddRange(CheckServiceLocatorPattern(root));
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckDirectInstantiation(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var objectCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
            foreach (var creation in objectCreations)
            {
                var parentClass = creation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (parentClass != null)
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "DI Violation: Direct Instantiation",
                        Message = $"Class '{parentClass.Identifier.Text}' directly instantiates '{creation.Type}'. Use dependency injection instead.",
                        Severity = "Warning",
                        LineNumber = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckMissingConstructorInjection(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var cls in classes)
            {
                var fields = cls.Members.OfType<FieldDeclarationSyntax>()
                    .Where(f => !f.Modifiers.Any(m => m.Text == "static"));
                var constructors = cls.Members.OfType<ConstructorDeclarationSyntax>();
                foreach (var field in fields)
                {
                    foreach (var variable in field.Declaration.Variables)
                    {
                        string fieldName = variable.Identifier.Text;
                        bool assignedInCtor = constructors.Any(ctor =>
                            ctor.Body != null &&
                            ctor.Body.Statements.OfType<ExpressionStatementSyntax>().Any(stmt =>
                                stmt.ToString().Contains(fieldName + " = ")));
                        if (!assignedInCtor)
                        {
                            results.Add(new CodeReviewResult
                            {
                                RuleName = "DI Violation: Missing Constructor Injection",
                                Message = $"Field '{fieldName}' in class '{cls.Identifier.Text}' is not assigned in any constructor. Consider using constructor injection.",
                                Severity = "Warning",
                                LineNumber = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                            });
                        }
                    }
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckServiceLocatorPattern(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                var exprStr = invocation.Expression.ToString();
                if (exprStr.Contains("GetService") || exprStr.Contains("GetRequiredService"))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "DI Violation: Service Locator",
                        Message = $"Service locator pattern detected: '{exprStr}'. Prefer constructor injection.",
                        Severity = "Warning",
                        LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }
    }
}
