using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ReviewSharpApp.Tests.TestHelpers
{
    public static class CodeParsing
    {
        public static CompilationUnitSyntax ParseCompilation(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            return tree.GetCompilationUnitRoot();
        }
        public static (CompilationUnitSyntax Root, SemanticModel Model) ParseAndGetSemanticModel(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetCompilationUnitRoot();

            // Reference necessary assemblies

           var coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Private.CoreLib.dll")),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location), // System.Runtime
            };
           
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees: [tree],
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            var model = compilation.GetSemanticModel(tree);
            return (root, model);
        }


    }
}
