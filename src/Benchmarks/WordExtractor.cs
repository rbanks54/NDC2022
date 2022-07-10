namespace Benchmarks;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.IO;
using UglyToad.PdfPig.Tokenization;
using UglyToad.PdfPig.Util;

public class WordExtractor
{

    private PdfDocument theIliad;
    IReadOnlyList<Letter> letters;
    IWordExtractor wordExtractor;
    IWordExtractor nn;
    IWordExtractor aPool;
    IWordExtractor v2;
    Consumer c = new();

    [GlobalSetup]
    public void Setup()
    {
        theIliad = PdfDocument.Open(@"C:\src\old_pdfpig\pdfs\homers illiad.pdf");
        letters = theIliad.GetPage(13).Letters;
        wordExtractor = DefaultWordExtractor.Instance;
        nn = NearestNeighbourWordExtractor.Instance;
        aPool = ArrayPoolWordExtractor.Instance;
        v2 = DefaultWordExtractor_v2.Instance;
    }

    [GlobalCleanup]
    public void CleanUp()
    {
        theIliad.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void Baseline()
    {
        wordExtractor.GetWords(letters).Consume(c);
    }

    [Benchmark]
    public void NearestNeigbour()
    {
        wordExtractor.GetWords(letters).Consume(c);
    }

    [Benchmark]
    public void ArrayPool()
    {
        aPool.GetWords(letters).Consume(c);
    }

    [Benchmark]
    public void V2_IgnoreZeroGaps()
    {
        v2.GetWords(letters).Consume(c);
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
