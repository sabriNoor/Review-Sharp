using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReviewSharp.Payload
{
    public class ReviewResultPayload
    {
        public Dictionary<string, List<ReviewSharp.Models.CodeReviewResult>> ResultsByFile { get; set; } = new();
        public Dictionary<string, string> FileCodes { get; set; } = new();
    }
}