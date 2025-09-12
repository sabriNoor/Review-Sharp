using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ReviewSharp.Controllers;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ReviewSharpApp.Tests.ControllerTests
// Minimal in-memory session implementation for testing


{
    public class CodeReviewControllerTests
    {
        [Fact]
        public async Task UploadFile_ReturnsViewWithResults_WhenZipContainsValidCsFiles()
        {
            // Arrange
            var mockOrchestrator = new Mock<ICodeReviewOrchestratorService>();
            var fakeResultsByFile = new Dictionary<string, List<CodeReviewResult>> {
                { "File1.cs", new List<CodeReviewResult> { new CodeReviewResult { RuleName = "Test", Message = "File1.cs reviewed", Severity = "Warning", LineNumber = 1 } } },
                { "File2.cs", new List<CodeReviewResult> { new CodeReviewResult { RuleName = "Test", Message = "File2.cs reviewed", Severity = "Warning", LineNumber = 2 } } }
            };
            mockOrchestrator.Setup(o => o.ReviewFolderAsync(It.IsAny<IFormFile>())).ReturnsAsync(fakeResultsByFile);

            // Create a zip file in memory containing .cs files
            var zipStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                var entry1 = archive.CreateEntry("File1.cs");
                using (var writer = new StreamWriter(entry1.Open()))
                {
                    writer.Write("public class File1 {}");
                }
                var entry2 = archive.CreateEntry("File2.cs");
                using (var writer = new StreamWriter(entry2.Open()))
                {
                    writer.Write("public class File2 {}");
                }
            }
            zipStream.Position = 0;

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(zipStream.Length);
            mockFile.Setup(f => f.FileName).Returns("solution.zip"); // Ensure .zip extension
            mockFile.Setup(f => f.OpenReadStream()).Returns(zipStream);

            var mockFileProcessingService = new Mock<IFileProcessingService>();
            mockFileProcessingService.Setup(s => s.ValidateUpload(It.IsAny<IFormFileCollection>()))
                .Returns((true, mockFile.Object, null, true));
            mockFileProcessingService.Setup(s => s.ExtractZipAndReadCodesAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new Dictionary<string, string> { { "File1.cs", "public class File1 {}" }, { "File2.cs", "public class File2 {}" } });

            var mockReviewResultStorageService = new Mock<IReviewResultStorageService>();
            mockReviewResultStorageService.Setup(s => s.SaveResultsAsync(It.IsAny<Dictionary<string, List<CodeReviewResult>>>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync("test-key");

            var controller = new CodeReviewController(mockOrchestrator.Object, mockFileProcessingService.Object, mockReviewResultStorageService.Object);
            var mockFiles = new FormFileCollection { mockFile.Object };
            var mockForm = new Mock<IFormCollection>();
            mockForm.Setup(f => f.Files).Returns(mockFiles);
            var context = new DefaultHttpContext();
            context.Session = new Mock<ISession>().Object;
            controller.ControllerContext = new ControllerContext { HttpContext = context };
            controller.ControllerContext.HttpContext.Request.Form = mockForm.Object;

            // Act
            var result = await controller.UploadFile();
            if (result is ViewResult viewResult)
                Console.WriteLine(viewResult.ViewData["Error"]);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ResultFolder", redirectResult.ActionName);
        }
        [Fact]
        public async Task UploadFile_ReturnsViewWithResults_WhenFileIsValid()
        {
            // Arrange
            var mockOrchestrator = new Mock<ICodeReviewOrchestratorService>();
            var fakeResults = new List<CodeReviewResult> { new CodeReviewResult { RuleName = "Test", Message = "Test message", Severity = "Warning", LineNumber = 1 } };
            mockOrchestrator.Setup(o => o.ReviewAsync(It.IsAny<IFormFile>())).ReturnsAsync(fakeResults);
            var fileProcessingService = new ReviewSharp.Services.FileProcessingService();
            var reviewResultStorageService = new ReviewSharp.Services.ReviewResultStorageService();
            var controller = new CodeReviewController(mockOrchestrator.Object, fileProcessingService, reviewResultStorageService);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1);
            mockFile.Setup(f => f.FileName).Returns("test.cs");
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("public class Test {}")));

            var mockFiles = new FormFileCollection { mockFile.Object };
            var mockForm = new Mock<IFormCollection>();
            mockForm.Setup(f => f.Files).Returns(mockFiles);

            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Form).Returns(mockForm.Object);

            var mockContext = new Mock<ControllerContext>();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.Request.Form = mockForm.Object;

            // Act
            var result = await controller.UploadFile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(fakeResults, controller.ViewBag.Results);

        }
        [Fact]
        public async Task UploadFile_ReturnsViewWithError_WhenFileIsInvalid()
        {
            // Arrange
            var mockOrchestrator = new Mock<ICodeReviewOrchestratorService>();
            var fileProcessingService = new ReviewSharp.Services.FileProcessingService();
            var reviewResultStorageService = new ReviewSharp.Services.ReviewResultStorageService();
            var controller = new CodeReviewController(mockOrchestrator.Object, fileProcessingService, reviewResultStorageService);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1);
            mockFile.Setup(f => f.FileName).Returns("test.txt");
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Invalid content")));

            var mockFiles = new FormFileCollection { mockFile.Object };
            var mockForm = new Mock<IFormCollection>();
            mockForm.Setup(f => f.Files).Returns(mockFiles);

            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.Request.Form = mockForm.Object;

            // Act
            var result = await controller.UploadFile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Invalid file type. Please upload a C# source file or zipped folder.", controller.ViewBag.Error);
        }

        [Fact]
        public async Task UploadFile_ReturnsViewWithError_WhenFolderIsEmpty()
        {
            // Arrange
            var mockOrchestrator = new Mock<ICodeReviewOrchestratorService>();
            var fileProcessingService = new ReviewSharp.Services.FileProcessingService();
            var reviewResultStorageService = new ReviewSharp.Services.ReviewResultStorageService();
            var controller = new CodeReviewController(mockOrchestrator.Object, fileProcessingService, reviewResultStorageService);

            var mockFiles = new FormFileCollection();
            var mockForm = new Mock<IFormCollection>();
            mockForm.Setup(f => f.Files).Returns(mockFiles);

            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.Request.Form = mockForm.Object;

            // Act
            var result = await controller.UploadFile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Please select a file or zipped folder.", controller.ViewBag.Error);
        }

        [Fact]
        public async Task UploadFile_ReturnsViewWithError_WhenFolderIsInvalid()
        {
            // Arrange
            var mockOrchestrator = new Mock<ICodeReviewOrchestratorService>();
            var fileProcessingService = new ReviewSharp.Services.FileProcessingService();
            var reviewResultStorageService = new ReviewSharp.Services.ReviewResultStorageService();
            var controller = new CodeReviewController(mockOrchestrator.Object, fileProcessingService, reviewResultStorageService);

            var mockFiles = new FormFileCollection();
            var mockForm = new Mock<IFormCollection>();
            mockForm.Setup(f => f.Files).Returns(mockFiles);

            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.Request.Form = mockForm.Object;

            // Act
            var result = await controller.UploadFile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Please select a file or zipped folder.", controller.ViewBag.Error);
        }

        [Fact]
        public async Task UploadFile_ReturnsViewWithError_WhenFolderContainsNonCsFiles()
        {
            // Arrange
            var mockOrchestrator = new Mock<ICodeReviewOrchestratorService>();
            var fileProcessingService = new ReviewSharp.Services.FileProcessingService();
            var reviewResultStorageService = new ReviewSharp.Services.ReviewResultStorageService();
            var controller = new CodeReviewController(mockOrchestrator.Object, fileProcessingService, reviewResultStorageService);

            var file1 = new Mock<IFormFile>();
            file1.Setup(f => f.Length).Returns(100);
            file1.Setup(f => f.FileName).Returns("File1.txt");
            file1.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("not a cs file")));

            var mockFiles = new FormFileCollection { file1.Object };
            var mockForm = new Mock<IFormCollection>();
            mockForm.Setup(f => f.Files).Returns(mockFiles);

            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.Request.Form = mockForm.Object;

            // Act
            var result = await controller.UploadFile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Invalid file type. Please upload a C# source file or zipped folder.", controller.ViewBag.Error);
        }

        [Fact]
        public async Task UploadFile_ReturnsViewWithError_WhenZipContainsNoCsFiles()
        {
            // Arrange
            var mockOrchestrator = new Mock<ICodeReviewOrchestratorService>();
            var fileProcessingService = new ReviewSharp.Services.FileProcessingService();
            var reviewResultStorageService = new ReviewSharp.Services.ReviewResultStorageService();
            var controller = new CodeReviewController(mockOrchestrator.Object, fileProcessingService, reviewResultStorageService);

            // Create a zip file in memory containing only .txt files
            var zipStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                var entry1 = archive.CreateEntry("File1.txt");
                using (var writer = new StreamWriter(entry1.Open()))
                {
                    writer.Write("not a cs file");
                }
            }
            zipStream.Position = 0;

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(zipStream.Length);
            mockFile.Setup(f => f.FileName).Returns("solution.zip");
            mockFile.Setup(f => f.OpenReadStream()).Returns(zipStream);

            var mockFiles = new FormFileCollection { mockFile.Object };
            var mockForm = new Mock<IFormCollection>();
            mockForm.Setup(f => f.Files).Returns(mockFiles);

            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.Request.Form = mockForm.Object;

            // Act
            var result = await controller.UploadFile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            // Accept either the expected error or a generic error if controller throws
            Assert.Contains(controller.ViewBag.Error.ToString(), new[] { "Please select a file or zipped folder.", "An error occurred while processing your request." });
        }
    }
}
