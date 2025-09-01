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
using ReviewSharp.Payload;
namespace ReviewSharp.Controllers
{
    public class CodeReviewController : Controller
    {
        private readonly ICodeReviewOrchestratorService _orchestratorService;
        private readonly IFileProcessingService _fileProcessingService;
        private readonly IReviewResultStorageService _reviewResultStorageService;

        public CodeReviewController(ICodeReviewOrchestratorService orchestratorService,
            IFileProcessingService fileProcessingService,
            IReviewResultStorageService reviewResultStorageService)
        {
            _orchestratorService = orchestratorService;
            _fileProcessingService = fileProcessingService;
            _reviewResultStorageService = reviewResultStorageService;
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
                    var fileCodes = await _fileProcessingService.ExtractZipAndReadCodesAsync(validatedFile);
                    var key = await _reviewResultStorageService.SaveResultsAsync(resultsByFile, fileCodes);
                    HttpContext.Session.SetString("ReviewResultKey", key);
                    ViewBag.UploadType = "Folder";
                    return RedirectToAction("ResultFolder", new { key });
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
        public async Task<IActionResult> ShowFileResult(string fileName)
        {
            try
            {
                var key = HttpContext.Session.GetString("ReviewResultKey") ?? (Request.Query["key"].ToString() ?? "");
                if (string.IsNullOrEmpty(key))
                {
                    TempData["Error"] = "Session expired or review results not found. Please re-upload your files.";
                    return RedirectToAction("Upload");
                }
                var payload = await _reviewResultStorageService.LoadResultsAsync(key);
                if (payload == null)
                {
                    TempData["Error"] = "Review results not found. Please re-upload your files.";
                    return RedirectToAction("Upload");
                }
                var normalizedFileName = System.Net.WebUtility.UrlDecode(fileName)
                    .Replace("\\", "/")
                    .ToLowerInvariant();
                if (!payload.ResultsByFile.ContainsKey(normalizedFileName) || !payload.FileCodes.ContainsKey(normalizedFileName))
                {
                    TempData["Error"] = "File not found in review results.";
                    return RedirectToAction("ResultFolder", new { key });
                }
                ViewBag.Results = payload.ResultsByFile[normalizedFileName];
                ViewBag.Code = payload.FileCodes[normalizedFileName];
                ViewBag.FileName = normalizedFileName;
                return View("Result");
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while retrieving the file results.";
                return RedirectToAction("Upload");
            }
        }


        private async Task<string> ReadFileContentAsync(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            return await reader.ReadToEndAsync();
        }

        [HttpGet]
        public async Task<IActionResult> ResultFolder(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                TempData["Error"] = "Session expired or review results not found. Please re-upload your files.";
                return RedirectToAction("Upload");
            }
            var payload = await _reviewResultStorageService.LoadResultsAsync(key);
            if (payload == null)
            {
                TempData["Error"] = "Review results not found. Please re-upload your files.";
                return RedirectToAction("Upload");
            }
            ViewBag.ResultsByFile = payload.ResultsByFile;
            ViewBag.FileCodes = payload.FileCodes;
            ViewBag.UploadType = "Folder";
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
