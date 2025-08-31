using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ReviewSharp.Services
{
    public class CodeReviewOrchestratorService : ICodeReviewOrchestratorService
    {
        private readonly ICodeParserService _parserService;
        private readonly IEnumerable<ICodeReviewService> _reviewServices;
        private readonly IEnumerable<ICodeReviewSemanticService> _semanticReviewServices;

        public CodeReviewOrchestratorService(
            ICodeParserService parserService,
            IEnumerable<ICodeReviewService> reviewServices,
            IEnumerable<ICodeReviewSemanticService> semanticReviewServices)
        {
            _parserService = parserService;
            _reviewServices = reviewServices;
            _semanticReviewServices = semanticReviewServices;
        }

        public async Task<List<Models.CodeReviewResult>> ReviewAsync(IFormFile file)
        {
            var compilation = await _parserService.ParseAsync(file);
            var syntaxTree = compilation.SyntaxTrees.First();
            var root = (CompilationUnitSyntax)await syntaxTree.GetRootAsync();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var results = new List<Models.CodeReviewResult>();
            if(root == null)
            {
                return results;
            }

            foreach (var service in _reviewServices)
            {
                results.AddRange(service.Review(root));
            }
            foreach (var service in _semanticReviewServices)
            {
                results.AddRange(service.Review(root, semanticModel));
            }

            return results;
        }

        public async Task<Dictionary<string, List<Models.CodeReviewResult>>> ReviewFolderAsync(IFormFile zipFile)
        {
            var resultsByFile = new Dictionary<string, List<Models.CodeReviewResult>>();
            if (zipFile == null || zipFile.Length == 0)
                return resultsByFile;

            // Save zip to temp location
            var tempZipPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempZipPath, FileMode.Create))
            {
                await zipFile.CopyToAsync(stream);
            }

            // Extract zip
            var tempExtractDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            System.IO.Directory.CreateDirectory(tempExtractDir);
            System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, tempExtractDir);

            // Find all .cs files
            var csFiles = Directory.GetFiles(tempExtractDir, "*.cs", SearchOption.AllDirectories);

            // Filter out auto-generated/non-reviewable files
            bool IsNonReviewable(string filePath, string fileName)
            {
                // Directory-based exclusions
                var dirs = new[] { "bin", "obj", "TestResults", "Generated", "Service References", "Migrations", "Areas\\Identity", "Pages\\Shared" };
                foreach (var dir in dirs)
                {
                    if (filePath.Contains(Path.DirectorySeparatorChar + dir + Path.DirectorySeparatorChar)) return true;
                }
                // File name patterns
                if (fileName.EndsWith(".Designer.cs") || fileName.EndsWith("ApplicationDbContextModelSnapshot.cs") || fileName == "Reference.cs" || fileName == "AssemblyInfo.cs" || fileName == "_ViewImports.cshtml.cs") return true;
                // Migration files
                if (fileName.Contains("_InitialCreate.cs") || fileName.Contains("_Migration.cs")) return true;
                return false;
            }

            foreach (var filePath in csFiles)
            {
                var fileName = Path.GetRelativePath(tempExtractDir, filePath);
                if (IsNonReviewable(filePath, fileName)) continue;
                var code = await File.ReadAllTextAsync(filePath);
                var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
                var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("CodeAnalysis")
                    .AddSyntaxTrees(syntaxTree)
                    .WithOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));
                var root = (CompilationUnitSyntax)await syntaxTree.GetRootAsync();
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var fileResults = new List<Models.CodeReviewResult>();
                foreach (var service in _reviewServices)
                {
                    fileResults.AddRange(service.Review(root));
                }
                foreach (var service in _semanticReviewServices)
                {
                    fileResults.AddRange(service.Review(root, semanticModel));
                }
                resultsByFile[fileName] = fileResults;
            }

            // Cleanup temp files
            try { File.Delete(tempZipPath); Directory.Delete(tempExtractDir, true); } catch { }

            return resultsByFile;
        }
    }
}
