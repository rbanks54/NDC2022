using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Geometry;

namespace Benchmarks
{
    public class PdfPoints
    {
        [Benchmark(Baseline = true)]
        public PdfPoint Point_UsingDecimals()
        {
            var p = new PdfPoint(0.534436m, 0.32552m);
            return p.MoveX(0.377281m);
        }

        [Benchmark]
        public PdfPoint_Double Point_UsingDoubles()
        {
            var p = new PdfPoint_Double(0.534436d, 0.32552d);
            return p.MoveX(0.377281);
        }

    }
}
