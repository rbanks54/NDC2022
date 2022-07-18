using System;
using System.Diagnostics;
using System.Linq;
using UglyToad.PdfPig;

namespace PerformanceTester
{
    class BenchmarkingEngine
    {
        private readonly string fileName;

        public BenchmarkingEngine(string fileName)
        {
            this.fileName = fileName;
        }

        public void Start()
        {
            var timer = Stopwatch.StartNew();

            using PdfDocument document = PdfDocument.Open(fileName);

            var structure = document.Structure;
            int pageCount = document.NumberOfPages;

            int wordCount = 0;
            for (var p = 1; p <= pageCount; p++)
            {
                var page = document.GetPage(p);
                wordCount += page.GetWords().Count();
            }

            timer.Stop();

            Console.WriteLine($"Pages: \t{pageCount}");
            Console.WriteLine($"Words: \t{wordCount}");
            Console.WriteLine($"Duration: \t{timer.Elapsed.TotalSeconds}s");
        }

    }
}
