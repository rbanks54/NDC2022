namespace Benchmarks {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using BenchmarkDotNet.Attributes;
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.IO;
    using UglyToad.PdfPig.Tokenization;
    public class GetPage {
        private static readonly PdfDocument TheIliad = PdfDocument.Open(@"C:\src\old_pdfpig\pdfs\homers illiad.pdf");

        [Params(1,20,50)]
        public int PageNumber { get; set; }

        [Benchmark(Baseline = true)]
        public void GetPage_Baseline()
        {
            TheIliad.GetPage(PageNumber);
        }

        //30% slower. Yikes!!
        // [Benchmark] 
        // [ArgumentsSource(nameof(TestNameStrings))] 
        // public void ValidNamesV2(StringConverter.Result input) 
        // { 
        //     tokenizer.TryTokenize_v2(input.First, input.Bytes, out var token); 
        // }

        //Faster, but v4 is even faster
        // [Benchmark] 
        // [ArgumentsSource(nameof(TestNameStrings))] 
        // public void ValidNames_v3(StringConverter.Result input) 
        // { 
        //     tokenizer.TryTokenize_v3(input.First, input.Bytes, out var token); 
        // }

        //[Benchmark] 
        //[ArgumentsSource(nameof(TestNameStrings))] 
        //public void ValidNames_v4(StringConverter.Result input) 
        //{ 
        //    tokenizer.TryTokenize_v4(input.First, input.Bytes, out var token); 
        //}

        //[Benchmark] 
        //[ArgumentsSource(nameof(TestNameStrings))] 
        //public void ValidNames_Latest(StringConverter.Result input) 
        //{ 
        //    tokenizer.TryTokenize(input.First, input.Bytes, out var token); 
        //}
    }
}
