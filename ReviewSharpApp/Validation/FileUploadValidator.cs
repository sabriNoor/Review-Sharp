using Microsoft.AspNetCore.Http;
using System.IO;

namespace ReviewSharp.Validation
{
    public static class FileUploadValidator
    {
        public static (bool IsValid, IFormFile? File, string? ErrorMessage, bool IsZip) Validate(IFormFileCollection files)
        {
            if (files.Count == 0)
                return (false, null, "Please select a file or zipped folder.", false);
            var file = files[0];
            if (file == null || file.Length == 0)
                return (false, null, "Please select a file or zipped folder.", false);
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension == ".zip")
                return (true, file, null, true);
            if (extension != ".cs")
                return (false, null, "Invalid file type. Please upload a C# source file or zipped folder.", false);
            return (true, file, null, false);
        }
    }
}