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
            var validation = FileUploadValidator.Validate(Request.Form.Files);
            if (!validation.IsValid)
            {
                ViewBag.Error = validation.ErrorMessage;
                return View("Upload");
            }

            var file = validation.File!;
            var code = await ReadFileContentAsync(file);
            var results = await _orchestratorService.ReviewAsync(file);
            ViewBag.Results = results;
            ViewBag.Code = code;
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
