using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReviewSharp.Services
{
    public class HardcodedSecretsService : ICodeReviewService
    {
        private static readonly Regex SecretPattern = new Regex(@"(password|secret|key|token|apiKey|accessKey|connectionString)", RegexOptions.IgnoreCase);
        private static readonly Regex ValuePattern = new Regex(@"[""']([A-Za-z0-9+/=]{8,})[""']");

        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            results.AddRange(CheckAllVariableDeclarators(root));
            return results;
        }

        private IEnumerable<CodeReviewResult> CheckAllVariableDeclarators(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var assignments = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();
            foreach (var variable in assignments)
            {
                var name = variable.Identifier.Text;
                var valueSyntax = variable.Initializer?.Value;
                var valueText = valueSyntax?.ToString() ?? string.Empty;

                // Skip if value is from configuration or environment
                if (valueSyntax != null && IsConfigurationBased(valueSyntax))
                    continue;

                if (IsPotentialSecret(name, valueText))
                {
                    results.Add(CreateResult("Variable", name, variable));
                }
            }
            return results;
        }

        private bool IsPotentialSecret(string name, string value)
        {
            return SecretPattern.IsMatch(name) && !string.IsNullOrEmpty(value) && ValuePattern.IsMatch(value);
        }
        // Helper to detect configuration/environment-based assignments
        private bool IsConfigurationBased(ExpressionSyntax valueSyntax)
        {
            if (valueSyntax == null) return false;
            var text = valueSyntax.ToString();
            // Common patterns for config/environment
            if (text.Contains("Configuration[") ||
                text.Contains("config[") ||
                text.Contains("GetSection(") ||
                text.Contains("GetValue(") ||
                text.Contains("GetConnectionString(") ||
                text.Contains("Environment.GetEnvironmentVariable") ||
                text.Contains("builder.Configuration[") ||
                text.Contains("appSettings[") ||
                text.Contains("Options.Value")
            )
                return true;
            return false;
        }
        

        private CodeReviewResult CreateResult(string type, string name, VariableDeclaratorSyntax variable)
        {
            return new CodeReviewResult
            {
                RuleName = "Hardcoded Secret/Password",
                Message = $"{type} '{name}' appears to contain a hardcoded secret or password.",
                Severity = "Critical",
                LineNumber = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            };
        }
    }

}
