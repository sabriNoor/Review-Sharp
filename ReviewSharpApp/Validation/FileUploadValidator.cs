using Microsoft.AspNetCore.Http;
using System.IO;

namespace ReviewSharp.Validation
{
    public static class FileUploadValidator
    {
        public static (bool IsValid, IFormFile? File, string? ErrorMessage) Validate(IFormFileCollection files)
        {
            if (files.Count == 0)
                return (false, null, "Please select a C# source file.");
            var file = files[0];
            if (file == null || file.Length == 0)
                return (false, null, "Please select a C# source file.");
            var extension = Path.GetExtension(file.FileName);
            if (extension != ".cs")
                return (false, null, "Invalid file type. Please upload a C# source file.");
            return (true, file, null);
        }
    }
}