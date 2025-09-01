using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReviewSharp.Services
{
    public class ReviewResultStorageService
    {
        private static string TempDir => Path.Combine(Path.GetTempPath(), "ReviewSharpResults");

        public ReviewResultStorageService()
        {
            if (!Directory.Exists(TempDir))
                Directory.CreateDirectory(TempDir);
        }

        public async Task<string> SaveResultsAsync(Dictionary<string, List<ReviewSharp.Models.CodeReviewResult>> resultsByFile, Dictionary<string, string> fileCodes)
        {
            var key = Guid.NewGuid().ToString("N");
            var filePath = Path.Combine(TempDir, $"review_{key}.json");
            var payload = new ReviewResultPayload { ResultsByFile = resultsByFile, FileCodes = fileCodes };
            var json = JsonSerializer.Serialize(payload);
            await File.WriteAllTextAsync(filePath, json);
            return key;
        }

        public async Task<ReviewResultPayload?> LoadResultsAsync(string key)
        {
            var filePath = Path.Combine(TempDir, $"review_{key}.json");
            if (!File.Exists(filePath)) return null;
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<ReviewResultPayload>(json);
        }

        public class ReviewResultPayload
        {
            public Dictionary<string, List<ReviewSharp.Models.CodeReviewResult>> ResultsByFile { get; set; } = new();
            public Dictionary<string, string> FileCodes { get; set; } = new();
        }
    }
}
