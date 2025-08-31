using Xunit;
using ReviewSharp.Services;
using ReviewSharp.Models;
using ReviewSharpApp.Tests.TestHelpers;
using System.Collections.Generic;

namespace ReviewSharpApp.Tests.ServiceTests
{
    public class EmptyCatchServiceTests
    {
        private static List<CodeReviewResult> GetResults(string source)
        {
            var service = new EmptyCatchService();
            var root = CodeParsing.ParseCompilation(source);
            return service.Review(root);
        }

        [Theory]
        [InlineData("class C { void M() { try { } catch { } } }", true)]
        [InlineData("class C { void M() { try { } catch (Exception) { } } }", true)]
        [InlineData("class C { void M() { try { } catch (Exception ex) { } } }", true)]
        [InlineData("class C { void M() { try { } catch { int x = 1; } } }", false)]
        [InlineData("class C { void M() { try { } catch { /* comment */ } } }", true)]
        [InlineData("class C { void M() { try { } catch { ; } } }", true)]
        public void Review_EmptyCatchBlock_WarnsIfEmpty(string code, bool shouldWarn)
        {
            var results = GetResults(code);
            if (shouldWarn)
                Assert.Contains(results, r => r.Message.Contains("Empty catch block"));
            else
                Assert.DoesNotContain(results, r => r.Message.Contains("Empty catch block"));
        }

        [Fact]
        public void Review_NullRoot_ReturnsEmpty()
        {
            var service = new EmptyCatchService();
            var results = service.Review(null);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_CatchWithHandling_NoWarning()
        {
            var code = "class C { void M() { try { } catch (Exception ex) { Console.WriteLine(ex); } } }";
            var results = GetResults(code);
            Assert.Empty(results);
        }

        [Fact]
        public void Review_MultipleCatchBlocks_AllReported()
        {
            var code = @"class C { void M() { try { } catch { } try { } catch (Exception) { } try { } catch { int x = 1; } } }";
            var results = GetResults(code);
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Empty catch block", r.Message));
        }
    }
}