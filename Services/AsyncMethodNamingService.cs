using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Linq;

namespace ReviewSharp.Services
{
    public class AsyncMethodNamingService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            results.AddRange(CheckAsyncMethodsInClasses(root));
            results.AddRange(CheckAsyncMethodsInInterfaces(root));
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckAsyncMethodsInClasses(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                bool isAsync = method.Modifiers.Any(m => m.Text == "async");
                string methodName = method.Identifier.Text;
                if (isAsync && !methodName.EndsWith("Async"))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Async Method Naming Convention",
                        Message = $"Async method '{methodName}' should have an 'Async' suffix.",
                        Severity = "Warning",
                        LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckAsyncMethodsInInterfaces(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var interfaceMethods = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()
                .SelectMany(i => i.Members.OfType<MethodDeclarationSyntax>());
            foreach (var method in interfaceMethods)
            {
                string methodName = method.Identifier.Text;
                string returnType = method.ReturnType.ToString();
                if ((returnType == "Task" || returnType.StartsWith("Task<")) && !methodName.EndsWith("Async"))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Async Method Naming Convention (Interface)",
                        Message = $"Interface async method '{methodName}' should have an 'Async' suffix.",
                        Severity = "Warning",
                        LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }
    }
}
