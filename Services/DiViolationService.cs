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
        // Constants for better maintainability
        private static readonly string[] DtoPatterns = {"dto", "model", "entity", "viewmodel", "request", "response", "command", "query",
            "record", "data", "contract", "schema", "vo", "aggregate"}; 
        private static readonly string[] SimpleTypes = {"string", "int", "long", "double", "decimal", "bool", "datetime", "guid", "uri", "timespan",
            "object", "byte", "short", "float", "char", "sbyte", "ushort", "uint", "ulong"}; 
        private static readonly string[] CollectionTypes = { "list", "array", "dictionary", "hashset", "queue", "stack", "icollection", "idictionary", "ienumerable" };
        private static readonly string[] ServicePatterns = { "service", "manager", "handler", "processor", "facade", "repository", "provider", "logger", "log",
            "client", "adapter", "factory", "builder", "validator", "authenticator", "authorizer", "mediator" };
        private static readonly string[] TestPatterns = { "test", "spec", "fixture", "mock", "stub" };
        private static readonly string[] ConfigPatterns = { "config", "options", "settings", "appsettings" };
        private static readonly string[] ConfigMethodPatterns = { "config", "setup", "configure" };

        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();

            results.AddRange(CheckDirectInstantiation(root));
            results.AddRange(CheckServiceLocatorPattern(root));

            return results;
        }

        private IEnumerable<CodeReviewResult> CheckDirectInstantiation(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var objectCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();

            foreach (var creation in objectCreations)
            {
                // Skip if this is inside a for loop initialization
                if (IsInForLoopInitialization(creation))
                    continue;

                // Skip if this is a DTO or simple data type
                if (IsDtoOrSimpleType(creation.Type))
                    continue;

                // Skip if this is a collection or array initialization
                if (IsCollectionOrArrayInitialization(creation))
                    continue;

                // Skip if this is a primitive type wrapper
                if (IsPrimitiveTypeWrapper(creation.Type))
                    continue;

                // Skip if this is in a test context
                if (IsInTestContext(creation))
                    continue;

                // Skip if this is configuration/options instantiation
                if (IsConfigurationInstantiation(creation))
                    continue;

                // Only flag if it's likely a service that should be injected
                if (IsLikelyService(creation.Type))
                {
                    var parentClass = creation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    if (parentClass != null)
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "DI Violation: Direct Instantiation",
                            Message = $"Class '{parentClass.Identifier.Text}' directly instantiates service '{creation.Type}'. Use dependency injection instead.",
                            Severity = "Warning",
                            LineNumber = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        });
                    }
                }
            }

            return results;
        }

        private bool IsInForLoopInitialization(ObjectCreationExpressionSyntax creation)
        {
            var forStatement = creation.Ancestors().OfType<ForStatementSyntax>().FirstOrDefault();
            if (forStatement == null) return false;

            return forStatement.Initializers.Any(init =>
                init.ToString().Contains(creation.ToString()));
        }

        private bool IsDtoOrSimpleType(TypeSyntax type)
        {
            var typeName = type.ToString().ToLower();

            // Common DTO patterns
            if (DtoPatterns.Any(pattern => typeName.Contains(pattern)))
                return true;

            // Simple data types that are typically not services
            if (SimpleTypes.Any(simple => typeName.Contains(simple)))
                return true;

            // Collections of simple types
            if (typeName.Contains("list<") || typeName.Contains("ienumerable<") ||
                typeName.Contains("array<") || typeName.Contains("dictionary<") ||
                typeName.Contains("icollection<") || typeName.Contains("idictionary<"))
                return true;

            // Configuration and options classes
            if (ConfigPatterns.Any(pattern => typeName.Contains(pattern)))
                return true;

            return false;
        }

        private bool IsCollectionOrArrayInitialization(ObjectCreationExpressionSyntax creation)
        {
            var typeName = creation.Type.ToString().ToLower();
            return CollectionTypes.Any(collection => typeName.Contains(collection));
        }

        private bool IsPrimitiveTypeWrapper(TypeSyntax type)
        {
            var typeName = type.ToString().ToLower();
            var primitiveWrappers = new[] { "string", "int", "long", "double", "decimal", "bool", "datetime", "guid" };
            return primitiveWrappers.Any(wrapper => typeName.Contains(wrapper));
        }

        private bool IsLikelyService(TypeSyntax type)
        {
            var typeName = type.ToString().ToLower();

            // Common service patterns
            if (ServicePatterns.Any(pattern => typeName.Contains(pattern)))
                return true;

            // Common interface patterns that indicate services
            if (typeName.StartsWith("i") && typeName.Length > 1 && char.IsUpper(typeName[1]))
                return true;

            return false;
        }

        private bool IsInTestContext(ObjectCreationExpressionSyntax creation)
        {
            // Check if this is in a test class
            var containingClass = creation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (containingClass != null)
            {
                var className = containingClass.Identifier.Text.ToLower();
                if (TestPatterns.Any(pattern => className.Contains(pattern)))
                    return true;
            }

            // Check if this is in a test namespace
            var containingNamespace = creation.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            if (containingNamespace != null)
            {
                var namespaceName = containingNamespace.Name.ToString().ToLower();
                if (namespaceName.Contains("test") || namespaceName.Contains("spec"))
                    return true;
            }

            return false;
        }

        private bool IsConfigurationInstantiation(ObjectCreationExpressionSyntax creation)
        {
            var typeName = creation.Type.ToString().ToLower();

            // Check if the type itself is configuration-related
            if (ConfigPatterns.Any(pattern => typeName.Contains(pattern)))
                return true;

            // Check if parent method is configuration-related
            var parentMethod = creation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (parentMethod != null)
            {
                var methodName = parentMethod.Identifier.Text.ToLower();
                if (ConfigMethodPatterns.Any(pattern => methodName.Contains(pattern)))
                    return true;
            }

            return false;
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
