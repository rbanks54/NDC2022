﻿namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;

    internal class DefaultWordExtractor : IWordExtractor
    {
        public IEnumerable<Word> GetWords(IReadOnlyList<Letter> letters)
        {
            var lettersOrder = letters.OrderByDescending(x => x.Location.Y)
                .ThenBy(x => x.Location.X);

            var lettersSoFar = new List<Letter>(10);

            var y = default(double?);
            var lastX = default(double?);
            var lastLetter = default(Letter);
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

                    lettersSoFar.Add(letter);
                    lastLetter = letter;
                    continue;
                }

                if (letter.Location.Y > y.Value + 0.5)
                {
                    if (lettersSoFar.Count > 0)
                    {
                        yield return GenerateWord(lettersSoFar);
                        lettersSoFar.Clear();
                    }

                    if (!string.IsNullOrWhiteSpace(letter.Value))
                    {
                        lettersSoFar.Add(letter);
                    }

                    y = letter.Location.Y;
                    lastX = letter.Location.X;
                    lastLetter = letter;

                    continue;
                }

                var gap = letter.Location.X - (lastLetter.Location.X + lastLetter.Width);
                var nextToLeft = letter.Location.X < lastX.Value - 1;
                var nextBigSpace = gap > Math.Max(lastLetter.GlyphRectangle.Height, letter.GlyphRectangle.Height) * 0.39;
                var nextIsWhiteSpace = string.IsNullOrWhiteSpace(letter.Value);
                var nextFontDiffers = !string.Equals(letter.FontName, lastLetter.FontName, StringComparison.OrdinalIgnoreCase) && gap > letter.Width * 0.1;
                var nextFontSizeDiffers = Math.Abs(letter.FontSize - lastLetter.FontSize) > 0.1;
                var nextTextDirectionDiffers = letter.TextDirection != lastLetter.TextDirection;

                if (nextToLeft || nextBigSpace || nextIsWhiteSpace || nextFontDiffers || nextFontSizeDiffers || nextTextDirectionDiffers)
                {
                    if (lettersSoFar.Count > 0)
                    {
                        yield return GenerateWord(lettersSoFar);
                        lettersSoFar.Clear();
                    }
                }

                if (!string.IsNullOrWhiteSpace(letter.Value))
                {
                    lettersSoFar.Add(letter);
                }

                lastLetter = letter;

                lastX = letter.Location.X;
            }

            if (lettersSoFar.Count > 0)
            {
                yield return GenerateWord(lettersSoFar);
            }
        }

        private static Word GenerateWord(List<Letter> letters)
        {
            return new Word(letters.ToList());
        }

        public static IWordExtractor Instance { get; } = new DefaultWordExtractor();

        private DefaultWordExtractor()
        {
        }
    }
}