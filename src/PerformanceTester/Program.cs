namespace PerformanceTester
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var pdf = @"C:\src\PdfPig\src\PerformanceTester\pdfs\homers illiad.pdf";
            // var pdf = @"C:\src\PdfPig\src\PerformanceTester\pdfs\War and Peace.pdf";

            new BenchmarkingEngine(pdf).Start();
        }
    }
}