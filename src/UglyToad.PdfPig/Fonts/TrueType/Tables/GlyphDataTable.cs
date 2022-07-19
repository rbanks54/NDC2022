﻿namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using System.Collections.Generic;
    using Geometry;
    using Glyphs;
    using Parser;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// The 'glyf' table contains the data that defines the appearance of the glyphs in the font. 
    /// This includes specification of the points that describe the contours that make up a glyph outline and the instructions that grid-fit that glyph.
    /// </summary>
    internal class GlyphDataTable : ITable
    {
        public string Tag => TrueTypeHeaderTable.Glyf;

        public TrueTypeHeaderTable DirectoryTable { get; }

        [ItemCanBeNull]
        public IReadOnlyList<IGlyphDescription> Glyphs { get; }

        public GlyphDataTable(TrueTypeHeaderTable directoryTable, IReadOnlyList<IGlyphDescription> glyphs)
        {
            DirectoryTable = directoryTable;
            Glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));
        }

        public static GlyphDataTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, TableRegister.Builder tableRegister)
        {
            data.Seek(table.Offset);

            var indexToLocationTable = tableRegister.IndexToLocationTable;

            var offsets = indexToLocationTable.GlyphOffsets;

            var entryCount = offsets.Length;

            var glyphCount = entryCount - 1;

            var glyphs = new IGlyphDescription[glyphCount];

            var emptyGlyph = Glyph.Empty(tableRegister.HeaderTable.Bounds);

            var compositeLocations = new Dictionary<int, TemporaryCompositeLocation>();

            for (var i = 0; i < glyphCount; i++)
            {
                if (offsets[i] == offsets[i + 1])
                {
                    // empty glyph
                    glyphs[i] = emptyGlyph;
                    continue;
                }

                data.Seek(offsets[i] + table.Offset);

                var contourCount = data.ReadSignedShort();

                var minX = data.ReadSignedShort();
                var minY = data.ReadSignedShort();
                var maxX = data.ReadSignedShort();
                var maxY = data.ReadSignedShort();
                
                var bounds = new PdfRectangle(minX, minY, maxX, maxY);
                
                // If the number of contours is greater than or equal zero it's a simple glyph.
                if (contourCount >= 0)
                {
                    glyphs[i] = ReadSimpleGlyph(data, contourCount, bounds);
                }
                else
                {
                    compositeLocations.Add(i , new TemporaryCompositeLocation(data.Position, bounds, contourCount));
                }
            }

            // Build composite glyphs by combining simple and other composite glyphs.
            foreach (var compositeLocation in compositeLocations)
            {
                glyphs[compositeLocation.Key] = ReadCompositeGlyph(data, compositeLocation.Value, compositeLocations, glyphs, emptyGlyph);
            }
            
            return new GlyphDataTable(table, glyphs);
        }

        private static Glyph ReadSimpleGlyph(TrueTypeDataBytes data, short contourCount, PdfRectangle bounds)
        {
            var endPointsOfContours = data.ReadUnsignedShortArray(contourCount);

            var instructionLength = data.ReadUnsignedShort();

            var instructions = data.ReadByteArray(instructionLength);

            var pointCount = 0;
            if (contourCount > 0)
            {
                pointCount = endPointsOfContours[contourCount - 1] + 1;
            }

            var flags = ReadFlags(data, pointCount);

            var xCoordinates = ReadCoordinates(data, pointCount, flags, SimpleGlyphFlags.XShortVector,
                SimpleGlyphFlags.XSignOrSame);

            var yCoordinates = ReadCoordinates(data, pointCount, flags, SimpleGlyphFlags.YShortVector,
                SimpleGlyphFlags.YSignOrSame);

            var points = new GlyphPoint[xCoordinates.Length];
            for (var i = xCoordinates.Length - 1; i >= 0; i--)
            {
                var isOnCurve = (flags[i] & SimpleGlyphFlags.OnCurve) == SimpleGlyphFlags.OnCurve;
                points[i] = new GlyphPoint(xCoordinates[i], yCoordinates[i], isOnCurve);
            }

            return new Glyph(true, instructions, endPointsOfContours, points, bounds);
        }

        private static IGlyphDescription ReadCompositeGlyph(TrueTypeDataBytes data, TemporaryCompositeLocation compositeLocation, Dictionary<int, TemporaryCompositeLocation> compositeLocations, IGlyphDescription[] glyphs,
            IGlyphDescription emptyGlyph)
        {
            bool HasFlag(CompositeGlyphFlags value, CompositeGlyphFlags target)
            {
                return (value & target) == target;
            }

            data.Seek(compositeLocation.Position);

            var components = new List<CompositeComponent>();
            
            // First recursively find all components and ensure they are available.
            CompositeGlyphFlags flags;
            do
            {
                flags = (CompositeGlyphFlags) data.ReadUnsignedShort();
                var glyphIndex = data.ReadUnsignedShort();

                var childGlyph = glyphs[glyphIndex];

                if (childGlyph == null)
                {
                    if (!compositeLocations.TryGetValue(glyphIndex, out var missingComposite))
                    {
                        throw new InvalidOperationException($"The composite glyph required a contour at index {glyphIndex} but there was no simple or composite glyph at this location.");
                    }

                    var position = data.Position;
                    childGlyph = ReadCompositeGlyph(data, missingComposite, compositeLocations, glyphs, emptyGlyph);
                    data.Seek(position);

                    glyphs[glyphIndex] = childGlyph;
                }
                
                short arg1, arg2;
                if (HasFlag(flags, CompositeGlyphFlags.Args1And2AreWords))
                {
                    arg1 = data.ReadSignedShort();
                    arg2 = data.ReadSignedShort();
                }
                else
                {
                    arg1 = data.ReadByte();
                    arg2 = data.ReadByte();
                }

                double xscale = 1;
                double scale01 = 0;
                double scale10 = 0;
                double yscale = 1;

                if (HasFlag(flags, CompositeGlyphFlags.WeHaveAScale))
                {
                    xscale = ReadTwoFourteenFormat(data);
                    yscale = xscale;
                }
                else if (HasFlag(flags, CompositeGlyphFlags.WeHaveAnXAndYScale))
                {
                    xscale = ReadTwoFourteenFormat(data);
                    yscale = ReadTwoFourteenFormat(data);
                }
                else if (HasFlag(flags, CompositeGlyphFlags.WeHaveATwoByTwo))
                {
                    xscale = ReadTwoFourteenFormat(data);
                    scale01 = ReadTwoFourteenFormat(data);
                    scale10 = ReadTwoFourteenFormat(data);
                    yscale = ReadTwoFourteenFormat(data);
                }

                if (HasFlag(flags, CompositeGlyphFlags.ArgsAreXAndYValues))
                {
                    components.Add(new CompositeComponent(glyphIndex, new PdfMatrix3By2(xscale, scale01, scale10, yscale, arg1, arg2)));
                }
                else
                {
                    // TODO: Not implemented, it is unclear how to do this.
                }

            } while (HasFlag(flags, CompositeGlyphFlags.MoreComponents));

            // Now build the final glyph from the components.
            IGlyphDescription builderGlyph = null;
            foreach (var component in components)
            {
                var glyph = glyphs[component.Index];

                var transformed = glyph.Transform(component.Transformation);

                if (builderGlyph == null)
                {
                    builderGlyph = transformed;
                }
                else
                {
                    builderGlyph = builderGlyph.Merge(transformed);
                }
            }

            builderGlyph = builderGlyph ?? emptyGlyph;

            return new Glyph(false, builderGlyph.Instructions, builderGlyph.EndPointsOfContours, builderGlyph.Points, compositeLocation.Bounds);
        }

        private static SimpleGlyphFlags[] ReadFlags(TrueTypeDataBytes data, int pointCount)
        {
            var result = new SimpleGlyphFlags[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                result[i] = (SimpleGlyphFlags)data.ReadByte();

                if (result[i].HasFlag(SimpleGlyphFlags.Repeat))
                {
                    var numberOfRepeats = data.ReadByte();

                    for (int j = 0; j < numberOfRepeats; j++)
                    {
                        result[i + j + 1] = result[i];
                    }

                    i += numberOfRepeats;
                }
            }

            return result;
        }

        private static short[] ReadCoordinates(TrueTypeDataBytes data, int pointCount, SimpleGlyphFlags[] flags, SimpleGlyphFlags isByte, SimpleGlyphFlags signOrSame)
        {
            var xs = new short[pointCount];
            var x = 0;
            for (var i = 0; i < pointCount; i++)
            {
                int dx;
                if (flags[i].HasFlag(isByte))
                {
                    var b = data.ReadByte();
                    dx = flags[i].HasFlag(signOrSame) ? b : -b;
                }
                else
                {
                    if (flags[i].HasFlag(signOrSame))
                    {
                        dx = 0;
                    }
                    else
                    {
                        dx = data.ReadSignedShort();
                    }
                }

                x += dx;

                xs[i] = (short)x;
            }

            return xs;
        }

        private static double ReadTwoFourteenFormat(TrueTypeDataBytes data)
        {
            const double divisor = 1 << 14;

            return data.ReadSignedShort() / divisor;
        }

        /// <summary>
        /// Stores the composite glyph information we read when initially scanning the glyph table.
        /// Once we have all composite glyphs we can start building them from simple glyphs.
        /// </summary>
        private struct TemporaryCompositeLocation
        {
            /// <summary>
            /// Stores the position after reading the contour count and bounds.
            /// </summary>
            public long Position { get; }
            
            public PdfRectangle Bounds { get; set; }
            
            public TemporaryCompositeLocation(long position, PdfRectangle bounds, short contourCount)
            {
                if (contourCount >= 0 )
                {
                    throw new ArgumentException($"A composite glyph should not have a positive contour count. Got: {contourCount}.", nameof(contourCount));
                }

                Position = position;
                Bounds = bounds;
            }
        }

        private class CompositeComponent
        {
            public int Index { get; }

            public PdfMatrix3By2 Transformation { get; }

            public CompositeComponent(int index, PdfMatrix3By2 transformation)
            {
                Index = index;
                Transformation = transformation;
            }
        }
    }
}
