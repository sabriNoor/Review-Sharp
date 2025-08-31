using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;

namespace ReviewSharp.Services
{
    public class DuplicateCodeService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();

            var methodNodes = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
            var localFunctionNodes = root.DescendantNodes().OfType<LocalFunctionStatementSyntax>().ToList();

            var allBlocks = new List<(string Key, string DisplayName, int LineNumber)>();

            foreach (var method in methodNodes)
            {
                var normalized = NormalizeMethod(method);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                var name = method.Identifier.Text;
                var line = method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                var key = ComputeHash(normalized);
                allBlocks.Add((key, name, line));
            }

            foreach (var local in localFunctionNodes)
            {
                var normalized = NormalizeLocalFunction(local);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                var name = local.Identifier.Text;
                var line = local.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                var key = ComputeHash(normalized);
                allBlocks.Add((key, name, line));
            }

            var duplicates = allBlocks
                .GroupBy(b => b.Key)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in duplicates)
            {
                var occurrences = group.OrderBy(g => g.LineNumber).ToList();
                // Build a concise message mentioning other duplicate locations
                var lines = string.Join(", ", occurrences.Select(o => o.LineNumber));
                foreach (var occ in occurrences)
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "DuplicateCode",
                        Severity = "Warning",
                        LineNumber = occ.LineNumber,
                        Message = $"Duplicate code detected. Same implementation appears at lines: {lines}. Consider refactoring to a shared method."
                    });
                }
            }

            return results;
        }

        private static string NormalizeMethod(MethodDeclarationSyntax method)
        {
            // Prefer body; fallback to expression body
            string raw = method.Body != null
                ? method.Body.ToFullString()
                : method.ExpressionBody?.Expression.ToFullString() ?? string.Empty;

            var bodyNode = method.Body as SyntaxNode ?? method.ExpressionBody as SyntaxNode;
            if (bodyNode == null)
            {
                return NormalizeCode(raw);
            }

            var parameterNames = method.ParameterList?.Parameters.Select(p => p.Identifier.Text).ToHashSet() ?? new HashSet<string>();
            var localNames = bodyNode.DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>()
                .SelectMany(ld => ld.Declaration.Variables.Select(v => v.Identifier.Text))
                .ToHashSet();
            var targetNames = new HashSet<string>(parameterNames.Concat(localNames));

            var canonical = CanonicalizeIdentifiers(bodyNode, targetNames);
            return NormalizeCode(canonical);
        }

        private static string NormalizeLocalFunction(LocalFunctionStatementSyntax local)
        {
            string raw = local.Body != null
                ? local.Body.ToFullString()
                : local.ExpressionBody?.Expression.ToFullString() ?? string.Empty;

            var bodyNode = local.Body as SyntaxNode ?? local.ExpressionBody as SyntaxNode;
            if (bodyNode == null)
            {
                return NormalizeCode(raw);
            }
            var parameterNames = local.ParameterList?.Parameters.Select(p => p.Identifier.Text).ToHashSet() ?? new HashSet<string>();
            var localNames = bodyNode.DescendantNodes()
                .OfType<LocalDeclarationStatementSyntax>()
                .SelectMany(ld => ld.Declaration.Variables.Select(v => v.Identifier.Text))
                .ToHashSet();
            var targetNames = new HashSet<string>(parameterNames.Concat(localNames));

            var canonical = CanonicalizeIdentifiers(bodyNode, targetNames);
            return NormalizeCode(canonical);
        }

        private static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return string.Empty;
            }

            // Simple normalization: lowercase, remove whitespace characters
            var sb = new StringBuilder(code.Length);
            foreach (var ch in code)
            {
                if (!char.IsWhiteSpace(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }
            return sb.ToString();
        }

        private static string CanonicalizeIdentifiers(SyntaxNode node, HashSet<string> targetNames)
        {
            var rewriter = new IdentifierCanonicalizer(targetNames);
            var rewritten = rewriter.Visit(node);
            return rewritten?.ToFullString() ?? node.ToFullString();
        }

        private sealed class IdentifierCanonicalizer : CSharpSyntaxRewriter
        {
            private readonly HashSet<string> _targetNames;
            private readonly Dictionary<string, string> _nameMap = new Dictionary<string, string>();
            private int _counter = 0;

            public IdentifierCanonicalizer(HashSet<string> targetNames)
            {
                _targetNames = targetNames;
            }

            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                var name = node.Identifier.Text;
                if (!_targetNames.Contains(name))
                {
                    return base.VisitIdentifierName(node);
                }

                if (!_nameMap.TryGetValue(name, out var canonical))
                {
                    _counter++;
                    canonical = $"v{_counter}";
                    _nameMap[name] = canonical;
                }

                return node.WithIdentifier(SyntaxFactory.Identifier(canonical));
            }
        }

        private static string ComputeHash(string text)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}


