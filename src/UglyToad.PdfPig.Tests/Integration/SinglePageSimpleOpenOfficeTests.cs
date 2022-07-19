namespace UglyToad.PdfPig.Tests.Integration
{
    using System.IO;
    using System.Linq;
    using Content;
    using Xunit;

    public class SinglePageSimpleOpenOfficeTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Assert.Equal(1, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Equal(PageSize.Letter, page.Size);
            }
        }

        [Fact]
        public void HasCorrectLetterBoundingBoxes()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                var comparer = new DoubleComparer(3);

                Assert.Equal("I", page.Letters[0].Value);

                Assert.Equal(90.1, page.Letters[0].GlyphRectangle.BottomLeft.X, comparer);
                Assert.Equal(709.2, page.Letters[0].GlyphRectangle.BottomLeft.Y, comparer);

                Assert.Equal(94.0, page.Letters[0].GlyphRectangle.TopRight.X, comparer);
                Assert.Equal(719.89, page.Letters[0].GlyphRectangle.TopRight.Y, comparer);

                Assert.Equal("a", page.Letters[5].Value);

                Assert.Equal(114.5, page.Letters[5].GlyphRectangle.BottomLeft.X, comparer);
                Assert.Equal(709.2, page.Letters[5].GlyphRectangle.BottomLeft.Y, comparer);

                Assert.Equal(119.82, page.Letters[5].GlyphRectangle.TopRight.X, comparer);
                Assert.Equal(714.89, page.Letters[5].GlyphRectangle.TopRight.Y, comparer);

                Assert.Equal("f", page.Letters[16].Value);

                Assert.Equal(169.9, page.Letters[16].GlyphRectangle.BottomLeft.X, comparer);
                Assert.Equal(709.2, page.Letters[16].GlyphRectangle.BottomLeft.Y, comparer);

                Assert.Equal(176.89, page.Letters[16].GlyphRectangle.TopRight.X, comparer);
                Assert.Equal(719.89, page.Letters[16].GlyphRectangle.TopRight.Y, comparer);
            }
        }

        [Fact]
        public void GetsCorrectPageTextIgnoringHiddenCharacters()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                var text = string.Join(string.Empty, page.Letters.Select(x => x.Value));

                Assert.Equal("I am a simple pdf.", text);
            }
        }
    }
}
