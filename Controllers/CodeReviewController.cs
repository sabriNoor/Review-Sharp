using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using ReviewSharp.Services;
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
            var file = Request.Form.Files[0];
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Please select a C# source file.";
                return View("Upload");
            }

            var results = await _orchestratorService.ReviewAsync(file);
            ViewBag.Results = results;
            return View("Result");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
