using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReviewSharpApp.Tests.TestHelpers;

public static class CodeParsing
{
    public static CompilationUnitSyntax ParseCompilation(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        return root;
    }
}