using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;

namespace Benchmarks
{
    public class Transformation_Matrix
    {
        [Benchmark(Baseline = true)]
        public void Matrix_With_Decimals()
        {
            var matrixA = TransformationMatrix.FromArray(new decimal[]
            {
                    3, 5, 7,
                    11, 13, -3,
                    17, -6, -9
            });
            var matrixB = TransformationMatrix.FromArray(new decimal[]
            {
                    5, 4, 3,
                    3, 7, 12,
                    1, 0, 6
            });

            var _ = matrixA.Multiply(matrixB);
        }

        [Benchmark]
        public void Matrix_With_Doubles()
        {
            var matrixA = TransformationMatrix_Double.FromArray(new double[]
            {
                    3, 5, 7,
                    11, 13, -3,
                    17, -6, -9
            });
            var matrixB = TransformationMatrix_Double.FromArray(new double[]
            {
                    5, 4, 3,
                    3, 7, 12,
                    1, 0, 6
            });

            var _ = matrixA.Multiply(matrixB);
        }

    }
}
