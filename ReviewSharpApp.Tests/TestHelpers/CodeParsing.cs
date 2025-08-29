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

    public static (CompilationUnitSyntax Root, SemanticModel Model) ParseAndGetSemanticModel(string source)
    {
        // Parse the syntax tree once
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        // Create a compilation with basic references
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location)
            )
            .AddSyntaxTrees(tree);

        var model = compilation.GetSemanticModel(tree);
        return (root, model);
    }
}
