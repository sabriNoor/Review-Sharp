using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ReviewSharp.Interfaces
{
    public interface ICodeParserService
    {
        Task<CompilationUnitSyntax> ParseAsync(IFormFile file);
    }
}
