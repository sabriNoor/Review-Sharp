using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReviewSharp.Payload;
namespace ReviewSharp.Interfaces
{
    public interface IReviewResultStorageService
    {
        Task<string> SaveResultsAsync(Dictionary<string, List<ReviewSharp.Models.CodeReviewResult>> resultsByFile, Dictionary<string, string> fileCodes);
        Task<ReviewResultPayload?> LoadResultsAsync(string key);
    }
}