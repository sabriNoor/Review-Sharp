namespace ReviewSharp.Models
{
    public class CodeReviewResult
    {
        public string RuleName { get; set; } = string.Empty;
        public string Message { get; set; }= string.Empty;
        public string Severity { get; set; } = string.Empty;
        public int? LineNumber { get; set; }
    }
}
