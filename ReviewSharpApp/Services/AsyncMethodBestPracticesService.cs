using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Linq;

namespace ReviewSharp.Services
{
    public class AsyncMethodBestPracticesService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                results.AddRange(CheckMissingAwait(method));
                results.AddRange(CheckAsyncVoid(method));
                results.AddRange(CheckBlockingCalls(method));
                results.AddRange(CheckAsyncCallWithoutAwait(method));
            }

            // Check interface async method return types
            var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            foreach (var iface in interfaces)
            {
                results.AddRange(CheckInterfaceAsyncMethods(iface));
            }

            return results;
        }
        /// Checks that async methods in interfaces return Task or Task<T>.
        private IEnumerable<CodeReviewResult> CheckInterfaceAsyncMethods(InterfaceDeclarationSyntax iface)
        {
            var results = new List<CodeReviewResult>();
            foreach (var method in iface.Members.OfType<MethodDeclarationSyntax>())
            {
                var methodName = method.Identifier.Text;
                // Heuristic: method name ends with Async
                if (methodName.EndsWith("Async"))
                {
                    var returnType = method.ReturnType.ToString();
                    if (!returnType.StartsWith("Task"))
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "Interface Async Method Return Type",
                            Message = $"Interface method '{methodName}' ends with 'Async' but does not return Task or Task<T>.",
                            Severity = "Warning",
                            LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        });
                    }
                }
            }
            return results;
        }


        private IEnumerable<CodeReviewResult> CheckAsyncCallWithoutAwait(MethodDeclarationSyntax method)
        {
            var results = new List<CodeReviewResult>();
            // Find all invocation expressions in the method
            var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                // Heuristic: method name ends with Async
                var invokedMethodName = invocation.Expression is MemberAccessExpressionSyntax memberAccess
                    ? memberAccess.Name.Identifier.Text
                    : invocation.Expression.ToString();
                if (invokedMethodName.EndsWith("Async"))
                {
                    // Check if parent is AwaitExpressionSyntax
                    var parent = invocation.Parent;
                    bool isAwaited = false;
                    while (parent != null)
                    {
                        if (parent is AwaitExpressionSyntax)
                        {
                            isAwaited = true;
                            break;
                        }
                        // Stop at statement level
                        if (parent is StatementSyntax)
                            break;
                        parent = parent.Parent;
                    }
                    if (!isAwaited)
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "Async Call Without Await",
                            Message = $"Async method '{invokedMethodName}' is called without 'await'. Consider awaiting the result.",
                            Severity = "Warning",
                            LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        });
                    }
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckMissingAwait(MethodDeclarationSyntax method)
        {
            var results = new List<CodeReviewResult>();
            bool isAsync = method.Modifiers.Any(m => m.Text == "async");
            string methodName = method.Identifier.Text;
            if (isAsync && !ContainsAwait(method))
            {
                results.Add(new CodeReviewResult
                {
                    RuleName = "Async Method Missing Await",
                    Message = $"Async method '{methodName}' does not contain any 'await' expression.",
                    Severity = "Warning",
                    LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                });
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckAsyncVoid(MethodDeclarationSyntax method)
        {
            var results = new List<CodeReviewResult>();
            bool isAsync = method.Modifiers.Any(m => m.Text == "async");
            string methodName = method.Identifier.Text;
            string returnType = method.ReturnType.ToString();
            if (isAsync && returnType == "void" && !IsEventHandler(method))
            {
                results.Add(new CodeReviewResult
                {
                    RuleName = "Async Method Returns Void",
                    Message = $"Async method '{methodName}' returns 'void'. Use 'Task' or 'Task<T>' instead (except for event handlers).",
                    Severity = "Warning",
                    LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                });
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckBlockingCalls(MethodDeclarationSyntax method)
        {
            var results = new List<CodeReviewResult>();
            bool isAsync = method.Modifiers.Any(m => m.Text == "async");
            string methodName = method.Identifier.Text;
            if (isAsync && ContainsBlockingCall(method))
            {
                results.Add(new CodeReviewResult
                {
                    RuleName = "Async Method Blocking Call",
                    Message = $"Async method '{methodName}' contains blocking calls like 'Wait()', 'Result', or 'GetAwaiter().GetResult()'. Avoid blocking in async methods.",
                    Severity = "Warning",
                    LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                });
            }
            return results;
        }

        private bool ContainsAwait(MethodDeclarationSyntax method)
        {
            return method.DescendantNodes().OfType<AwaitExpressionSyntax>().Any();
        }

        private bool IsEventHandler(MethodDeclarationSyntax method)
        {
            var parameters = method.ParameterList.Parameters;
            return parameters.Count > 0 && parameters.Last().Type != null && parameters.Last().Type!.ToString().EndsWith("EventArgs");
        }

        private bool ContainsBlockingCall(MethodDeclarationSyntax method)
        {
            var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                var exprStr = invocation.Expression.ToString();
                if (exprStr.EndsWith(".Wait") || exprStr.EndsWith(".Result") || exprStr.Contains("GetAwaiter().GetResult"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
