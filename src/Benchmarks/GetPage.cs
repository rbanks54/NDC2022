using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Benchmarks;

public class GetPage
{
    private PdfDocument? theIliad;

    [GlobalSetup]
    public void Setup()
    {
        theIliad = PdfDocument.Open(@"C:\src\PdfPig\src\PerformanceTester\pdfs\homers illiad.pdf");
    }

    [GlobalCleanup]
    public void CleanUp()
    {
        theIliad?.Dispose();
    }

    //[Params(1, 20, 50)]
    [Params(50)]
    public int PageNumber { get; set; }

    [Benchmark(Baseline = true)]
    public Page Baseline()
    {
        return theIliad!.GetPage(PageNumber);
    }
}