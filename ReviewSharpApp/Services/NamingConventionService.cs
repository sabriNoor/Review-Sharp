using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class NamingConventionService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            results.AddRange(CheckClassNaming(root));
            results.AddRange(CheckInterfaceNaming(root));
            results.AddRange(CheckEnumNaming(root));
            results.AddRange(CheckEnumMemberNaming(root));
            results.AddRange(CheckLocalVariableNaming(root));
            results.AddRange(CheckGenericTypeParameterNaming(root));
            results.AddRange(CheckEventHandlerNaming(root));
            results.AddRange(CheckDelegateTypeNaming(root));
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckGenericTypeParameterNaming(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var typeParams = root.DescendantNodes().OfType<TypeParameterSyntax>();
            foreach (var param in typeParams)
            {
                string paramName = param.Identifier.Text;
                if (paramName.Length != 1 || !char.IsUpper(paramName[0]))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Generic Type Parameter Naming Convention",
                        Message = $"Generic type parameter '{paramName}' should be a single uppercase letter (e.g., 'T').",
                        Severity = "Info",
                        LineNumber = param.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckEventHandlerNaming(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                string methodName = method.Identifier.Text;
                // Heuristic: event handlers often have EventArgs as last parameter
                var parameters = method.ParameterList.Parameters;
                if (parameters.Count > 0 && parameters.Last().Type != null && parameters.Last().Type!.ToString().EndsWith("EventArgs"))
                {
                    if (!methodName.EndsWith("Handler") && !methodName.StartsWith("On"))
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "Event Handler Naming Convention",
                            Message = $"Event handler '{methodName}' should start with 'On' or end with 'Handler'.",
                            Severity = "Info",
                            LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        });
                    }
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckDelegateTypeNaming(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var delegates = root.DescendantNodes().OfType<DelegateDeclarationSyntax>();
            foreach (var del in delegates)
            {
                string delName = del.Identifier.Text;
                if (!delName.EndsWith("Handler") && !delName.EndsWith("Callback"))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Delegate Type Naming Convention",
                        Message = $"Delegate type '{delName}' should end with 'Handler' or 'Callback'.",
                        Severity = "Info",
                        LineNumber = del.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckClassNaming(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var cls in classes)
            {
                string className = cls.Identifier.Text;
                if (!IsPascalCase(className))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Class Naming Convention",
                        Message = $"Class '{className}' should be PascalCase.",
                        Severity = "Warning",
                        LineNumber = cls.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }

                results.AddRange(CheckFieldNaming(cls));
                results.AddRange(CheckPropertyNaming(cls));
                results.AddRange(CheckMethodAndParameterNaming(cls));
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckFieldNaming(ClassDeclarationSyntax cls)
        {
            var results = new List<CodeReviewResult>();
            var fields = cls.DescendantNodes().OfType<FieldDeclarationSyntax>();
            foreach (var field in fields)
            {
                bool isConst = field.Modifiers.Any(m => m.Text == "const");
                foreach (var variable in field.Declaration.Variables)
                {
                    string fieldName = variable.Identifier.Text;
                    bool isPrivate = field.Modifiers.Any(m => m.Text == "private") || !field.Modifiers.Any(m => m.Text == "public" || m.Text == "protected");
                    if (isConst && !IsAllCaps(fieldName))
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "Constant Naming Convention",
                            Message = $"Constant '{fieldName}' should be ALL_CAPS.",
                            Severity = "Warning",
                            LineNumber = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        });
                    }
                    else if (isPrivate && !isConst && !IsPrivateFieldConvention(fieldName))
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "Field Naming Convention",
                            Message = $"Private field '{fieldName}' should be '_camelCase'.",
                            Severity = "Warning",
                            LineNumber = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        });
                    }
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckPropertyNaming(ClassDeclarationSyntax cls)
        {
            var results = new List<CodeReviewResult>();
            var properties = cls.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var prop in properties)
            {
                string propName = prop.Identifier.Text;
                if (!IsPascalCase(propName))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Property Naming Convention",
                        Message = $"Property '{propName}' should be PascalCase.",
                        Severity = "Warning",
                        LineNumber = prop.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckMethodAndParameterNaming(ClassDeclarationSyntax cls)
        {
            var results = new List<CodeReviewResult>();
            var methods = cls.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                results.AddRange(CheckSingleMethodNaming(method));
                results.AddRange(CheckParameterNaming(method));
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckSingleMethodNaming(MethodDeclarationSyntax method)
        {
            var results = new List<CodeReviewResult>();
            string methodName = method.Identifier.Text;
            if (!IsPascalCase(methodName))
            {
                results.Add(new CodeReviewResult
                {
                    RuleName = "Method Naming Convention",
                    Message = $"Method '{methodName}' should be PascalCase.",
                    Severity = "Warning",
                    LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                });
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckParameterNaming(MethodDeclarationSyntax method)
        {
            var results = new List<CodeReviewResult>();
            foreach (var param in method.ParameterList.Parameters)
            {
                string paramName = param.Identifier.Text;
                if (!IsCamelCase(paramName))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Parameter Naming Convention",
                        Message = $"Parameter '{paramName}' should be camelCase.",
                        Severity = "Info",
                        LineNumber = param.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckInterfaceNaming(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            foreach (var iface in interfaces)
            {
                string ifaceName = iface.Identifier.Text;
                if (!IsPascalCase(ifaceName) || !ifaceName.StartsWith("I"))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Interface Naming Convention",
                        Message = $"Interface '{ifaceName}' should be PascalCase and start with 'I'.",
                        Severity = "Error",
                        LineNumber = iface.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckEnumNaming(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var enums = root.DescendantNodes().OfType<EnumDeclarationSyntax>();
            foreach (var enm in enums)
            {
                string enumName = enm.Identifier.Text;
                if (!IsPascalCase(enumName))
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Enum Naming Convention",
                        Message = $"Enum '{enumName}' should be PascalCase.",
                        Severity = "Warning",
                        LineNumber = enm.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckLocalVariableNaming(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var variables = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();
            foreach (var variable in variables)
            {
                var parent = variable.Parent?.Parent;
                if (parent is LocalDeclarationStatementSyntax)
                {
                    string varName = variable.Identifier.Text;
                    if (!IsCamelCase(varName))
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "Local Variable Naming Convention",
                            Message = $"Local variable '{varName}' should be camelCase.",
                            Severity = "Info",
                            LineNumber = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        });
                    }
                }
            }
            return results;
        }

         private IEnumerable<CodeReviewResult> CheckEnumMemberNaming(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var enums = root.DescendantNodes().OfType<EnumDeclarationSyntax>();
            foreach (var enm in enums)
            {
                foreach (var member in enm.Members)
                {
                    string memberName = member.Identifier.Text;
                    if (!IsPascalCase(memberName))
                    {
                        results.Add(new CodeReviewResult
                        {
                            RuleName = "Enum Member Naming Convention",
                            Message = $"Enum member '{memberName}' should be PascalCase.",
                            Severity = "Warning",
                            LineNumber = member.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        });
                    }
                }
            }
            return results;
        }

        private bool IsPascalCase(string name)
        {
            return !string.IsNullOrEmpty(name) && char.IsUpper(name[0]) && !name.Contains("_");
        }

        private bool IsCamelCase(string name)
        {
            return !string.IsNullOrEmpty(name) && char.IsLower(name[0]) && !name.Contains("_");
        }

        private bool IsPrivateFieldConvention(string name)
        {
            return name.StartsWith("_") && name.Length > 1 && char.IsLower(name[1]);
        }

        private bool IsAllCaps(string name)
        {
            return !string.IsNullOrEmpty(name) && name.All(c => char.IsUpper(c) || c == '_');
        }
    }
}
