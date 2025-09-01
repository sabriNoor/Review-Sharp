using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReviewSharp.Interfaces
{
    public interface IFileProcessingService
    {
        (bool IsValid, IFormFile? File, string? ErrorMessage, bool IsZip) ValidateUpload(IFormFileCollection files);
        Task<Dictionary<string, string>> ExtractZipAndReadCodesAsync(IFormFile zipFile);
        Task<string> ReadFileContentAsync(IFormFile file);
    }
}