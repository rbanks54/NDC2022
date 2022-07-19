﻿namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Core;
    using Fonts;
    using Fonts.Encodings;
    using Geometry;
    using Tokens;

    internal class Standard14WritingFont : IWritingFont
    {
        private readonly FontMetrics metrics;

        public bool HasWidths { get; } = false;

        public string Name => metrics.FontName;

        public Standard14WritingFont(FontMetrics metrics)
        {
            this.metrics = metrics;
        }

        public bool TryGetBoundingBox(char character, out PdfRectangle boundingBox)
        {
            var encoding = StandardEncoding.Instance;
            boundingBox = default(PdfRectangle);
            if (!metrics.CharacterMetrics.TryGetValue(encoding.GetName(character), out var characterMetric))
            {
                return false;
            }

            boundingBox = new PdfRectangle(characterMetric.BoundingBox.Left, characterMetric.BoundingBox.Bottom,
                characterMetric.BoundingBox.Left + characterMetric.WidthX, characterMetric.BoundingBox.Top);

            return true;
        }

        public bool TryGetAdvanceWidth(char character, out double width)
        {
            width = 0;
            if (!TryGetBoundingBox(character, out var bbox))
            {
                return false;
            }

            width = bbox.Width;

            return true;
        }

        public TransformationMatrix GetFontMatrix()
        {
            return TransformationMatrix.FromValues(1/1000d, 0, 0, 1/1000d, 0, 0);
        }

        public ObjectToken WriteFont(NameToken fontKeyName, Stream outputStream, BuilderContext context)
        {
            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Font },
                { NameToken.Subtype, NameToken.Type1  },
                { NameToken.BaseFont, NameToken.Create(metrics.FontName) },
                { NameToken.Encoding, NameToken.MacRomanEncoding },
                { NameToken.Name, fontKeyName }
            };

            var token = new DictionaryToken(dictionary);

            var result = context.WriteObject(outputStream, token);

            return result;
        }
    }

    internal class BuilderContext
    {
        private readonly List<int> reservedNumbers = new List<int>();

        public int CurrentNumber { get; private set; } = 1;

        private readonly Dictionary<IndirectReference, long> objectOffsets = new Dictionary<IndirectReference, long>();
        public IReadOnlyDictionary<IndirectReference, long> ObjectOffsets => objectOffsets;

        public ObjectToken WriteObject(Stream stream, IToken token, int? reservedNumber = null)
        {
            int number;
            if (reservedNumber.HasValue)
            {
                if (!reservedNumbers.Remove(reservedNumber.Value))
                {
                    throw new InvalidOperationException();
                }

                number = reservedNumber.Value;
            }
            else
            {
                number = CurrentNumber++;
            }

            var reference = new IndirectReference(number, 0);
            var obj = new ObjectToken(stream.Position, reference, token);
            objectOffsets.Add(reference, obj.Position);
            TokenWriter.WriteToken(obj, stream);
            return obj;
        }

        public int ReserveNumber()
        {
            var reserved = CurrentNumber;
            reservedNumbers.Add(reserved);
            CurrentNumber++;
            return reserved;
        }
    }
}