using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using ReviewSharp.Services;
using ReviewSharp.Validation;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ReviewSharp.Controllers
{
    public class CodeReviewController : Controller
    {
        private readonly ICodeReviewOrchestratorService _orchestratorService;

        public CodeReviewController(ICodeReviewOrchestratorService orchestratorService)
        {
            _orchestratorService = orchestratorService;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile()
        {
            try
            {
                var validationResult = FileUploadValidator.Validate(Request.Form.Files);
                bool isValid = validationResult.IsValid;
                var validatedFile = validationResult.File;
                var errorMessage = validationResult.ErrorMessage;
                bool isZip = validationResult.IsZip;
                if (!isValid || validatedFile == null)
                {
                    ViewBag.Error = errorMessage ?? "Please select a file or zipped folder.";
                    return View("Upload");
                }

                if (isZip)
                {
                    var resultsByFile = await _orchestratorService.ReviewFolderAsync(validatedFile);
                    ViewBag.ResultsByFile = resultsByFile;
                    ViewBag.UploadType = "Folder";

                    // Extract zip and get code for each file
                    var tempZipPath = Path.GetTempFileName();
                    using (var stream = new FileStream(tempZipPath, FileMode.Create))
                    {
                        await validatedFile.CopyToAsync(stream);
                    }
                    var tempExtractDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    System.IO.Directory.CreateDirectory(tempExtractDir);
                    System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, tempExtractDir);
                    var csFiles = Directory.GetFiles(tempExtractDir, "*.cs", SearchOption.AllDirectories);
                    var fileCodes = new Dictionary<string, string>();
                    foreach (var filePath in csFiles)
                    {
                        var fileName = Path.GetRelativePath(tempExtractDir, filePath);
                        var code = await System.IO.File.ReadAllTextAsync(filePath);
                        fileCodes[fileName] = code;
                    }
                    // Serialize to Session
                    HttpContext.Session.SetString("ResultsByFile", System.Text.Json.JsonSerializer.Serialize(resultsByFile));
                    HttpContext.Session.SetString("FileCodes", System.Text.Json.JsonSerializer.Serialize(fileCodes));
                    try { System.IO.File.Delete(tempZipPath); System.IO.Directory.Delete(tempExtractDir, true); } catch { }

                    return View("ResultFolder");
                }
                else
                {
                    string code;
                    using (var reader = new StreamReader(validatedFile.OpenReadStream()))
                    {
                        code = await reader.ReadToEndAsync();
                    }
                    var results = await _orchestratorService.ReviewAsync(validatedFile);
                    ViewBag.Results = results;
                    ViewBag.Code = code;
                    ViewBag.UploadType = "SingleFile";
                    return View("Result");
                }
            }
            catch (Exception)
            {
                // Log the exception (not implemented here)
                ViewBag.Error = "An error occurred while processing your request.";
                return View("Upload");
            }
        }

        [HttpGet]
        public IActionResult ShowFileResult(string fileName)
        {
            // Deserialize from Session
            var resultsByFileJson = HttpContext.Session.GetString("ResultsByFile");
            var fileCodesJson = HttpContext.Session.GetString("FileCodes");
            if (string.IsNullOrEmpty(resultsByFileJson) || string.IsNullOrEmpty(fileCodesJson))
            {
                TempData["Error"] = "File not found in review results.";
                return RedirectToAction("ResultFolder");
            }
            var resultsByFile = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<CodeReviewResult>>>(resultsByFileJson);
            var fileCodes = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(fileCodesJson);
            if (resultsByFile == null || fileCodes == null || !resultsByFile.ContainsKey(fileName) || !fileCodes.ContainsKey(fileName))
            {
                TempData["Error"] = "File not found in review results.";
                return RedirectToAction("ResultFolder");
            }
            ViewBag.Results = resultsByFile[fileName];
            ViewBag.Code = fileCodes[fileName];
            ViewBag.FileName = fileName;
            return View("Result");
        }


        private async Task<string> ReadFileContentAsync(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            return await reader.ReadToEndAsync();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
