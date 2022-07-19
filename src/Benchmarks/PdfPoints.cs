using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Geometry;

namespace Benchmarks
{
    public class PdfPoints
    {
        [Benchmark(Baseline = true)]
        public PdfPoint Point_UsingDecimals()
        {
            var p = new PdfPoint(0.534436, 0.32552);
            return p.MoveX(0.377281);
        }

        //[Benchmark]
        //public PdfPoint_Double Point_UsingDoubles()
        //{
        //    var p = new PdfPoint_Double(0.534436d, 0.32552d);
        //    return p.MoveX(0.377281);
        //}

    }
}
