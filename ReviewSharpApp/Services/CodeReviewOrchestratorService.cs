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
    }
}
