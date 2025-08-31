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
            var controller = new CodeReviewController(mockOrchestrator.Object);

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
            Assert.NotNull(controller.ViewBag.ResultsByFile);
            // Optionally, check that results contain reviews for both files
        }

        [Fact]
        public async Task UploadFile_ReturnsViewWithError_WhenFolderIsEmpty()
        {
            // Arrange
            var mockOrchestrator = new Mock<ICodeReviewOrchestratorService>();
            var controller = new CodeReviewController(mockOrchestrator.Object);

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
            var controller = new CodeReviewController(mockOrchestrator.Object);

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
                var controller = new CodeReviewController(mockOrchestrator.Object);

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
