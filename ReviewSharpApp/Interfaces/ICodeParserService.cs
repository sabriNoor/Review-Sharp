using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;

namespace ReviewSharp.Interfaces
{
    public interface ICodeParserService
    {
        Task<Compilation> ParseAsync(IFormFile file);
    }
}
