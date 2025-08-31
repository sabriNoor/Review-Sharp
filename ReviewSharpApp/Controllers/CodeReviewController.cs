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
    private readonly IFileProcessingService _fileProcessingService;

        public CodeReviewController(ICodeReviewOrchestratorService orchestratorService
        , IFileProcessingService fileProcessingService)
        {
            _orchestratorService = orchestratorService;
            _fileProcessingService = fileProcessingService;
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
                var validationResult = _fileProcessingService.ValidateUpload(Request.Form.Files);
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

                    var fileCodes = await _fileProcessingService.ExtractZipAndReadCodesAsync(validatedFile);
                    HttpContext.Session.SetString("ResultsByFile", System.Text.Json.JsonSerializer.Serialize(resultsByFile));
                    HttpContext.Session.SetString("FileCodes", System.Text.Json.JsonSerializer.Serialize(fileCodes));
                    return View("ResultFolder");
                }
                else
                {
                    var code = await _fileProcessingService.ReadFileContentAsync(validatedFile);
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
            try
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
            catch (Exception)
            {
                // Log the exception (not implemented here)
                TempData["Error"] = "An error occurred while retrieving the file results.";
                return RedirectToAction("ResultFolder");
            }
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
