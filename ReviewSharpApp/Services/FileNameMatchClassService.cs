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

            // Iterate over all class declarations
            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
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
