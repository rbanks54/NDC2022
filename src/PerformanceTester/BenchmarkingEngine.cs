using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Util;
using UglyToad.PdfPig.Writer;

namespace PerformanceTester
{
    class BenchmarkingEngine
    {
        private readonly string[] files;
        private readonly string testName;

        public BenchmarkingEngine(string[] files, string testName)
        {
            this.files = files;
            this.testName = testName;
        }

        public void Start()
        {
            var timer = Stopwatch.StartNew();

            //for (int i = 0; i < 10; i++)
            //{
            foreach (var file in files)
            {
                using (PdfDocument document = PdfDocument.Open(file))
                {
                    int pageCount = document.NumberOfPages;

                    Page page = document.GetPage(1);

                    var extractor = DefaultWordExtractor_v2.Instance;
                    extractor.GetWords(page.Letters).Count();


                    var widthInPoints = page.Width;
                    var heightInPoints = page.Height;

                    int wordCount = 0;
                    for (var p = 0; p < document.NumberOfPages; p++)
                    {
                        // This starts at 1 rather than 0.
                        page = document.GetPage(p + 1);

                        wordCount += page.GetWords().Count();
                    }
                }
            }
            //}

            timer.Stop();

            Console.WriteLine($"{testName}: - Duration time: \t{timer.Elapsed.TotalSeconds}s");
        }

        public void StartCreate()
        {
            var timer = Stopwatch.StartNew();
            PdfDocumentBuilder builder = new PdfDocumentBuilder();

            var page = builder.AddPage(PageSize.A4);

            var pageHeight = page.PageSize.Height;
            var pageWidth = page.PageSize.Width;

            page.DrawLine(new PdfPoint(30, 520), new PdfPoint(360, 520));
            page.DrawLine(new PdfPoint(360, 520), new PdfPoint(360, 250));

            page.SetStrokeColor(250, 132, 131);
            page.DrawLine(new PdfPoint(25, 70), new PdfPoint(100, 70), 3);
            page.ResetColor();
            page.DrawRectangle(new PdfPoint(30, 200), 250, 100, 0.5m);
            page.DrawRectangle(new PdfPoint(30, 100), 250, 100, 0.5m);

            page = builder.AddPage(PageSize.A4);

            var fontFile = @"C:\WIndows\Fonts\Calist.ttf"; //Callisto

            var font = builder.AddTrueTypeFont(File.ReadAllBytes(fontFile));

            var letters = page.MeasureText("Mortal's guide to making a pig run faster", 12, new PdfPoint(30, 50), font);
            var lineHeight = letters.Max(l => l.GlyphRectangle.Height);
            var baseline = pageHeight - lineHeight - 30;

            for (int i = 0; i < 10_000; i++)
            {
                if (baseline <= 30)
                {
                    page = builder.AddPage(PageSize.A4);
                    baseline = pageHeight - lineHeight - 30;
                }
                page.AddText("Mortal's guide to making a pig run faster", 12, new PdfPoint(30, baseline), font);
                baseline -= (lineHeight + 5);
            }

            var b = builder.Build();

            var tempFileName = Path.GetTempFileName();
            File.WriteAllBytes(tempFileName, b);

            Console.WriteLine($"{testName}: - Duration time: \t{timer.Elapsed.TotalSeconds}s. Output: {tempFileName}");
        }

    }
}
