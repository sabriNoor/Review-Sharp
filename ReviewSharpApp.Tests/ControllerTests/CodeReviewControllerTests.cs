using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ReviewSharp.Controllers;
using ReviewSharp.Interfaces;
using ReviewSharp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ReviewSharpApp.Tests.ControllerTests
{
    public class CodeReviewControllerTests
    {

        [Fact(Skip ="old version")]
        public async Task UploadFile_ReturnsViewWithResults_WhenFileIsValid()
        {
            // Arrange
            var mockOrchestrator = new Mock<ICodeReviewOrchestratorService>();
            var fakeResults = new List<CodeReviewResult> { new CodeReviewResult { RuleName = "Test", Message = "Test message", Severity = "Warning", LineNumber = 1 } };
            mockOrchestrator.Setup(o => o.ReviewAsync(It.IsAny<IFormFile>())).ReturnsAsync(fakeResults);
            var controller = new CodeReviewController(mockOrchestrator.Object);

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
            Assert.Equal(fakeResults, viewResult.ViewData["Results"]);

        }
        [Fact(Skip ="old version")]
        public async Task UploadFile_ReturnsViewWithError_WhenFileIsNull()
        {
            // Arrange
            var mockOrchestrator = new Mock<ICodeReviewOrchestratorService>();
            var controller = new CodeReviewController(mockOrchestrator.Object);

            var mockForm = new Mock<IFormCollection>();
            mockForm.Setup(f => f.Files).Returns(new FormFileCollection());

            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Form).Returns(mockForm.Object);

            var mockContext = new Mock<ControllerContext>();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.Request.Form = mockForm.Object;

            // Act
            var result = await controller.UploadFile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Please select a C# source file.", controller.ViewBag.Error);
        }
        [Fact(Skip ="old version")]
        public async Task UploadFile_ReturnsViewWithError_WhenFileIsInvalidType()
        {
            // Arrange
            var mockOrchestrator = new Mock<ICodeReviewOrchestratorService>();
            var controller = new CodeReviewController(mockOrchestrator.Object);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1);
            mockFile.Setup(f => f.FileName).Returns("test.txt");
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Invalid content")));

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
            Assert.Equal("Invalid file type. Please upload a C# source file.", controller.ViewBag.Error);
        }
    }
}
