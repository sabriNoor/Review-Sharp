using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;


namespace ReviewSharp.Services
{
    public class FileNameMatchClassService : ICodeReviewService
    {
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var results = new List<CodeReviewResult>();

            // Get the file name without extension
            var syntaxTree = root.SyntaxTree;
            var filePath = syntaxTree.FilePath;
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            // Only check top-level (non-nested) class declarations
            var topLevelClasses = root.Members.OfType<ClassDeclarationSyntax>();
            foreach (var classDecl in topLevelClasses)
            {
                var className = classDecl.Identifier.Text;
                if (className != fileName)
                {
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "File Name Mismatch",
                        Message = $"Class name '{className}' does not match file name '{fileName}'.",
                        Severity = "Warning",
                        LineNumber = classDecl.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }

            return results;
        }
    }
}
