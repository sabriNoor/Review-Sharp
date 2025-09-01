using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;


namespace ReviewSharp.Services
{
    public class FileNameMatchClassService : ICodeReviewService
    {
        // For interface compatibility
        public List<CodeReviewResult> Review(CompilationUnitSyntax root)
        {
            var syntaxTree = root.SyntaxTree;
            var filePath = syntaxTree.FilePath;
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return ReviewWithOriginal(root, fileName);
        }

        public List<CodeReviewResult> ReviewWithOriginal(CompilationUnitSyntax root, string originalFileName)
        {
            var results = new List<CodeReviewResult>();

            // Only check top-level (non-nested) class declarations
            var classDecls = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDecl in classDecls)
            {
                var className = classDecl.Identifier.Text;
                if (className != originalFileName)
                {
                    var syntaxTree = root.SyntaxTree;
                    var filePath = syntaxTree.FilePath;
                    results.Add(new CodeReviewResult
                    {
                        RuleName = "File Name Mismatch",
                        Message = $"Class name '{className}' does not match file name '{originalFileName}' (file path: '{filePath}').",
                        Severity = "Warning",
                        LineNumber = classDecl.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }

            return results;
        }
    }
}
