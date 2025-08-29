using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.AspNetCore.Http;
using ReviewSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReviewSharp.Services
{
    public class CodeParserService : ICodeParserService
    {
        public async Task<Compilation> ParseAsync(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var code = await reader.ReadToEndAsync();
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

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


            var compilation = CSharpCompilation.Create("CodeAnalysis")
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTree)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation;
        }
    }
}
