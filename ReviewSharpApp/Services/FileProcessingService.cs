using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ReviewSharp.Services
{
    public interface IFileProcessingService
    {
        (bool IsValid, IFormFile? File, string? ErrorMessage, bool IsZip) ValidateUpload(IFormFileCollection files);
        Task<Dictionary<string, string>> ExtractZipAndReadCodesAsync(IFormFile zipFile);
        Task<string> ReadFileContentAsync(IFormFile file);
    }

    public class FileProcessingService : IFileProcessingService
    {
        public (bool IsValid, IFormFile? File, string? ErrorMessage, bool IsZip) ValidateUpload(IFormFileCollection files)
        {
            return Validation.FileUploadValidator.Validate(files);
        }

        public async Task<Dictionary<string, string>> ExtractZipAndReadCodesAsync(IFormFile zipFile)
        {
            var fileCodes = new Dictionary<string, string>();
            var tempZipPath = Path.GetTempFileName();
            using (var stream = new FileStream(tempZipPath, FileMode.Create))
            {
                await zipFile.CopyToAsync(stream);
            }
            var tempExtractDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempExtractDir);
            System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, tempExtractDir);
            var csFiles = Directory.GetFiles(tempExtractDir, "*.cs", SearchOption.AllDirectories);
            foreach (var filePath in csFiles)
            {
                var fileName = Path.GetRelativePath(tempExtractDir, filePath);
                var code = await File.ReadAllTextAsync(filePath);
                fileCodes[fileName] = code;
            }
            try { File.Delete(tempZipPath); Directory.Delete(tempExtractDir, true); } catch { }
            return fileCodes;
        }

        public async Task<string> ReadFileContentAsync(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            return await reader.ReadToEndAsync();
        }
    }
}
