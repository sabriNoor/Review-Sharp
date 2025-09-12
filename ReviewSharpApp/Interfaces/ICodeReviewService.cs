using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReviewSharp.Interfaces
{
    public interface ICodeReviewService
    {
        List<Models.CodeReviewResult> Review(CompilationUnitSyntax root);
    }
}
