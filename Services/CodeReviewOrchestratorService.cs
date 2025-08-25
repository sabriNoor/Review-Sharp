using Microsoft.AspNetCore.Http;
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

        public CodeReviewOrchestratorService(ICodeParserService parserService, IEnumerable<ICodeReviewService> reviewServices)
        {
            _parserService = parserService;
            _reviewServices = reviewServices;
        }

        public async Task<List<Models.CodeReviewResult>> ReviewAsync(IFormFile file)
        {
            var root = await _parserService.ParseAsync(file);
            var results = new List<Models.CodeReviewResult>();

            foreach (var service in _reviewServices)
            {
                var serviceResults = service.Review(root);
                if (serviceResults != null && serviceResults.Count > 0)
                {
                    results.AddRange(serviceResults);
                }
            }

            return results;
        }
    }
}
