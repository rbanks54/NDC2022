namespace UglyToad.PdfPig.Tests.Geometry
{
    using PdfPig.Geometry;
    using Xunit;

    public class PdfVectorTests
    {
        [Fact]
        public void ConstructorSetsValues()
        {
            var vector = new PdfVector(5.2, 6.9);

            Assert.Equal(5.2, vector.X);
            Assert.Equal(6.9, vector.Y);
        }

        [Fact]
        public void ScaleMultipliesLeavesOriginalUnchanged()
        {
            var vector = new PdfVector(5.2, 6.9);

            var scaled = vector.Scale(0.7);

            Assert.Equal(5.2, vector.X);
            Assert.Equal(5.2 * 0.7, scaled.X);

            Assert.Equal(6.9, vector.Y);
            Assert.Equal(6.9 * 0.7, scaled.Y);
        }
    }
}
