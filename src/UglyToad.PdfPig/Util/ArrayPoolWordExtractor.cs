namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using Content;

    /// <summary>
    /// Default Word Extractor.
    /// </summary>
    public class ArrayPoolWordExtractor : IWordExtractor
    {
        /// <summary>
        /// Gets the words.
        /// </summary>
        /// <param name="letters">The letters in the page.</param>
        public IEnumerable<Word> GetWords(IReadOnlyList<Letter> letters)
        {
            var lettersOrder = letters.OrderByDescending(x => x.Location.Y)
                .ThenBy(x => x.Location.X);

            //Switch this to an array pool
            //do we need to handle words longer than 64 characters?
            //Note: 64 chars to fit with 'easy' memory boundaries
            var lettersSoFar = ArrayPool<Letter>.Shared.Rent(64); 

            //var lettersSoFar = new List<Letter>(10);

            var gapCountsSoFarByFontSize = new Dictionary<double, Dictionary<double, int>>();

            var y = default(double?);
            var lastX = default(double?);
            var lastLetter = default(Letter);
            var letterIndex = 0;
            foreach (var letter in lettersOrder)
            {
                if (!y.HasValue)
                {
                    y = letter.Location.Y;
                }

                if (!lastX.HasValue)
                {
                    lastX = letter.Location.X;
                }

                if (lastLetter == null)
                {
                    if (string.IsNullOrWhiteSpace(letter.Value))
                    {
                        continue;
                    }

                    lettersSoFar[letterIndex++] = letter;
                    lastLetter = letter;
                    continue;
                }

                if (letter.Location.Y > y.Value + 0.5)
                {
                    if (letterIndex > 0)
                    {
                        yield return GenerateWord(lettersSoFar, letterIndex);
                        ArrayPool<Letter>.Shared.Return(lettersSoFar);
                        letterIndex = 0;
                    }

                    if (!string.IsNullOrWhiteSpace(letter.Value))
                    {
                        lettersSoFar[letterIndex++] = letter;
                    }

                    y = letter.Location.Y;
                    lastX = letter.Location.X;
                    lastLetter = letter;

                    continue;
                }

                var letterHeight = Math.Max(lastLetter.GlyphRectangle.Height, letter.GlyphRectangle.Height);

                var gap = letter.Location.X - (lastLetter.Location.X + lastLetter.Width);
                var nextToLeft = letter.Location.X < lastX.Value - 1;
                var nextBigSpace = gap > letterHeight * 0.39;
                var nextIsWhiteSpace = string.IsNullOrWhiteSpace(letter.Value);
                var nextFontDiffers = !string.Equals(letter.FontName, lastLetter.FontName, StringComparison.OrdinalIgnoreCase) && gap > letter.Width * 0.1;
                var nextFontSizeDiffers = Math.Abs(letter.FontSize - lastLetter.FontSize) > 0.1;
                var nextTextOrientationDiffers = letter.TextOrientation != lastLetter.TextOrientation;

                var suspectGap = false;

                if (!nextFontSizeDiffers && letter.FontSize > 0 && gap >= 0)
                {
                    var fontSize = Math.Round(letter.FontSize);
                    if (!gapCountsSoFarByFontSize.TryGetValue(fontSize, out var gapCounts))
                    {
                        gapCounts = new Dictionary<double, int>();
                        gapCountsSoFarByFontSize[fontSize] = gapCounts;
                    }

                    var gapRounded = Math.Round(gap, 2);
                    if (!gapCounts.ContainsKey(gapRounded))
                    {
                        gapCounts[gapRounded] = 0;
                    }

                    gapCounts[gapRounded]++;

                    // More than one type of gap.
                    if (gapCounts.Count > 1 && gap > letterHeight * 0.16)
                    {
                        var mostCommonGap = gapCounts.OrderByDescending(x => x.Value).First();

                        if (gap > (mostCommonGap.Key * 5) && mostCommonGap.Value > 1)
                        {
                            suspectGap = true;
                        }
                    }
                }

                if (nextToLeft || nextBigSpace || nextIsWhiteSpace || nextFontDiffers || nextFontSizeDiffers || nextTextOrientationDiffers || suspectGap)
                {
                    if (letterIndex > 0)
                    {
                        yield return GenerateWord(lettersSoFar, letterIndex);
                        ArrayPool<Letter>.Shared.Return(lettersSoFar);
                        letterIndex = 0;
                    }
                }

                if (!string.IsNullOrWhiteSpace(letter.Value))
                {
                    lettersSoFar[letterIndex++] = letter;
                }

                lastLetter = letter;

                lastX = letter.Location.X;
            }

            if (letterIndex > 0)
            {
                yield return GenerateWord(lettersSoFar, letterIndex);
                ArrayPool<Letter>.Shared.Return(lettersSoFar);
                letterIndex = 0;
            }
        }

        private static Word GenerateWord(Letter[] letters, int length)
        {
            if (length == 0) return new Word(new Letter[] { });
            return new Word(letters[0..length]);
        }

        /// <summary>
        /// Create an instance of Default Word Extractor, <see cref="DefaultWordExtractor"/>.
        /// </summary>
        public static IWordExtractor Instance { get; } = new ArrayPoolWordExtractor();

        private ArrayPoolWordExtractor()
        {
        }
    }
}