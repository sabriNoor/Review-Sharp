using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReviewSharp.Interfaces
{
    public interface ICodeReviewOrchestratorService
    {
        Task<List<Models.CodeReviewResult>> ReviewAsync(IFormFile file);
        Task<Dictionary<string, List<Models.CodeReviewResult>>> ReviewFolderAsync(IFormFile zipFile);
    }
}
