using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Linq;

namespace ReviewSharp.Services
{
    public class NestedBlockDepthService : ICodeReviewService
    {
        private const int MaxAllowedDepth = 3; // You can adjust this threshold

        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                int maxDepth = GetMaxBlockDepth(method.Body);
                if (maxDepth > MaxAllowedDepth)
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "Nested Block Depth",
                        Message = $"Method '{method.Identifier.Text}' contains nested blocks with depth {maxDepth}, which exceeds the allowed maximum of {MaxAllowedDepth}.",
                        Severity = "Warning",
                        LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
            return results;
        }

        private int GetMaxBlockDepth(BlockSyntax? block, int currentDepth = 1)
        {
            if (block == null)
                return 0;
            int maxDepth = currentDepth;
            foreach (var statement in block.Statements)
            {
                if (statement is BlockSyntax innerBlock)
                {
                    int depth = GetMaxBlockDepth(innerBlock, currentDepth + 1);
                    if (depth > maxDepth)
                        maxDepth = depth;
                }
                else if (statement is IfStatementSyntax ifStmt)
                {
                    int depth = GetMaxBlockDepth(ifStmt.Statement as BlockSyntax, currentDepth + 1);
                    if (depth > maxDepth)
                        maxDepth = depth;
                    if (ifStmt.Else != null)
                    {
                        int elseDepth = GetMaxBlockDepth(ifStmt.Else.Statement as BlockSyntax, currentDepth + 1);
                        if (elseDepth > maxDepth)
                            maxDepth = elseDepth;
                    }
                }
                else if (statement is ForStatementSyntax forStmt)
                {
                    int depth = GetMaxBlockDepth(forStmt.Statement as BlockSyntax, currentDepth + 1);
                    if (depth > maxDepth)
                        maxDepth = depth;
                }
                else if (statement is WhileStatementSyntax whileStmt)
                {
                    int depth = GetMaxBlockDepth(whileStmt.Statement as BlockSyntax, currentDepth + 1);
                    if (depth > maxDepth)
                        maxDepth = depth;
                }
                else if (statement is DoStatementSyntax doStmt)
                {
                    int depth = GetMaxBlockDepth(doStmt.Statement as BlockSyntax, currentDepth + 1);
                    if (depth > maxDepth)
                        maxDepth = depth;
                }
                else if (statement is SwitchStatementSyntax switchStmt)
                {
                    foreach (var section in switchStmt.Sections)
                    {
                        foreach (var switchStatement in section.Statements)
                        {
                            if (switchStatement is BlockSyntax switchBlock)
                            {
                                int depth = GetMaxBlockDepth(switchBlock, currentDepth + 1);
                                if (depth > maxDepth)
                                    maxDepth = depth;
                            }
                        }
                    }
                }
            }
            return maxDepth;
        }
    }
}
