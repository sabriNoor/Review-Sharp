using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using ReviewSharp.Interfaces;

namespace ReviewSharp.Services
{
    public class CodeParserService : ICodeParserService
    {
        public async Task<CompilationUnitSyntax> ParseAsync(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var code = await reader.ReadToEndAsync();
                var tree = CSharpSyntaxTree.ParseText(code);
                return (CompilationUnitSyntax)tree.GetRoot();
            }
        }
    }
}
