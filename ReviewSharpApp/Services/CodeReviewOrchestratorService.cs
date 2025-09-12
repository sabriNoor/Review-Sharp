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
            if (root == null)
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

            // Parse each file using parserService, but build a shared compilation for semantic analysis
            var syntaxTrees = new List<SyntaxTree>();
            var fileMap = new Dictionary<string, (SyntaxTree SyntaxTree, string OriginalFileName)>();
            foreach (var filePath in csFiles)
            {
                var normalizedFileName = Path.GetRelativePath(tempExtractDir, filePath)
                    .Replace("\\", "/")
                    .ToLowerInvariant();
                var originalFileName = Path.GetFileNameWithoutExtension(filePath);
                if (IsNonReviewable(filePath, normalizedFileName)) continue;
                try
                {
                    // Read file into memory for FormFile
                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    using var memStream = new MemoryStream(fileBytes);
                    var formFile = new FormFile(memStream, 0, memStream.Length, normalizedFileName, normalizedFileName);
                    var compilation = await _parserService.ParseAsync(formFile);
                    var syntaxTree = compilation.SyntaxTrees.FirstOrDefault();
                    if (syntaxTree != null)
                    {
                        syntaxTrees.Add(syntaxTree);
                        fileMap[normalizedFileName] = (syntaxTree, originalFileName);
                    }
                }
                catch
                {
                    // Optionally log or collect errors per file
                    continue;
                }
            }

            // Build a single compilation for all files
            var sharedCompilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("CodeAnalysis")
                .AddSyntaxTrees(syntaxTrees)
                .WithOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

            // Review each file using the shared compilation
            foreach (var kv in fileMap)
            {
                var fileName = kv.Key;
                var syntaxTree = kv.Value.SyntaxTree;
                var originalFileName = kv.Value.OriginalFileName;
                var root = (CompilationUnitSyntax)await syntaxTree.GetRootAsync();
                var semanticModel = sharedCompilation.GetSemanticModel(syntaxTree);
                var fileResults = new List<Models.CodeReviewResult>();
                foreach (var service in _reviewServices)
                {
                    if (service is FileNameMatchClassService fileNameMatchService)
                    {
                        fileResults.AddRange(fileNameMatchService.ReviewWithOriginal(root, originalFileName));
                    }
                    else
                    {
                        fileResults.AddRange(service.Review(root));
                    }
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
