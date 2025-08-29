using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using ReviewSharp.Interfaces;
using Microsoft.CodeAnalysis;

namespace ReviewSharp.Services
{
    public class CodeParserService : ICodeParserService
    {
        public async Task<Compilation> ParseAsync(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var code = await reader.ReadToEndAsync();
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create("CodeAnalysis")
                    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                    .AddSyntaxTrees(syntaxTree);

                return compilation;

            }
        }
    }
}
