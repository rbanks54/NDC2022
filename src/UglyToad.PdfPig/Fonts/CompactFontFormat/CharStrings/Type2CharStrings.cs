﻿namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dictionaries;
    using Geometry;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Stores the decoded command sequences for Type 2 CharStrings from a Compact Font Format font as well
    /// as the local (per font) and global (per font set) subroutines.
    /// The CharStrings are lazily evaluated.
    /// </summary>
    internal class Type2CharStrings
    {
        private readonly object locker = new object();
        private readonly Dictionary<string, Type2Glyph> glyphs = new Dictionary<string, Type2Glyph>();

        /// <summary>
        /// The decoded charstrings in this font.
        /// </summary>
        public IReadOnlyDictionary<string, CommandSequence> CharStrings { get; }

        /// <summary>
        /// The indexed bytes for the local subroutines in this font.
        /// </summary>
        [NotNull]
        public CompactFontFormatIndex LocalSubroutines { get; }

        /// <summary>
        /// The indexed bytes for the global subroutines in this font set.
        /// </summary>
        [NotNull]
        public CompactFontFormatIndex GlobalSubroutines { get; }

        public Type2CharStrings(IReadOnlyDictionary<string, CommandSequence> charStrings, CompactFontFormatIndex localSubroutines,
            CompactFontFormatIndex globalSubroutines)
        {
            CharStrings = charStrings ?? throw new ArgumentNullException(nameof(charStrings));
            LocalSubroutines = localSubroutines ?? throw new ArgumentNullException(nameof(localSubroutines));
            GlobalSubroutines = globalSubroutines ?? throw new ArgumentNullException(nameof(globalSubroutines));
        }

        /// <summary>
        /// Evaluate the CharString for the character with a given name returning the path constructed for the glyph.
        /// </summary>
        /// <param name="name">The name of the character to retrieve the CharString for.</param>
        /// <returns>A <see cref="PdfPath"/> for the glyph.</returns>
        public Type2Glyph Generate(string name)
        {
            Type2Glyph glyph;
            lock (locker)
            {
                if (glyphs.TryGetValue(name, out var result))
                {
                    return result;
                }

                if (!CharStrings.TryGetValue(name, out var sequence))
                {
                    throw new InvalidOperationException($"No charstring sequence with the name /{name} in this font.");
                }

                try
                {
                    glyph = Run(sequence);

                    glyphs[name] = glyph;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to interpret charstring for symbol with name: {name}. Commands: {sequence}.", ex);
                }
            }

            return glyph;
        }

        private static Type2Glyph Run(CommandSequence sequence)
        {
            var context = new Type2BuildCharContext();

            var hasRunStackClearingCommand = false;
            foreach (var command in sequence.Commands)
            {
                command.Match(x => context.Stack.Push(x),
                   x =>
                   {
                       if (!hasRunStackClearingCommand)
                       {
                           /*
                            * The first stack-clearing operator, which must be one of hstem, hstemhm, vstem, vstemhm, cntrmask, hintmask, hmoveto, vmoveto,
                            * rmoveto, or endchar, takes an additional argument — the width (as described earlier), which may be expressed as zero or one numeric argument.
                            */
                           hasRunStackClearingCommand = true;
                           switch (x.Name)
                           {
                               case "hstem":
                               case "hstemhm":
                               case "vstemhm":
                               case "vstem":
                                   {
                                       var oddArgCount = context.Stack.Length % 2 != 0;
                                       if (oddArgCount)
                                       {
                                           context.Width = context.Stack.PopBottom();
                                       }
                                       break;
                                   }
                               case "hmoveto":
                               case "vmoveto":
                                   SetWidthFromArgumentsIfPresent(context, 1);
                                   break;
                               case "rmoveto":
                                   SetWidthFromArgumentsIfPresent(context, 2);
                                   break;
                               case "cntrmask":
                               case "hintmask":
                               case "endchar:":
                                   SetWidthFromArgumentsIfPresent(context, 0);
                                   break;
                               default:
                                   hasRunStackClearingCommand = false;
                                   break;

                           }

                       }
                       x.Run(context);
                   });
            }

            return new Type2Glyph(context.Path, context.Width);
        }

        private static void SetWidthFromArgumentsIfPresent(Type2BuildCharContext context, int expectedArgumentLength)
        {
            if (context.Stack.Length > expectedArgumentLength)
            {
                context.Width = context.Stack.PopBottom();
            }
        }

        public class CommandSequence
        {
            /// <summary>
            /// The ordered list of numbers and commands for a Type 2 charstring or subroutine.
            /// </summary>
            public IReadOnlyList<Union<double, LazyType2Command>> Commands { get; }

            public CommandSequence(IReadOnlyList<Union<double, LazyType2Command>> commands)
            {
                Commands = commands ?? throw new ArgumentNullException(nameof(commands));
            }

            public override string ToString()
            {
                return string.Join(", ", Commands.Select(x => x.ToString()));
            }
        }
    }

    /// <summary>
    /// Since Type 2 CharStrings may define their width as the first argument (as a delta from the font's nominal width X)
    /// we can retrieve both details for the Type 2 glyph.
    /// </summary>
    internal class Type2Glyph
    {
        /// <summary>
        /// The path of the glyph.
        /// </summary>
        [NotNull]
        public PdfPath Path { get; }

        /// <summary>
        /// The width of the glyph as a difference from the nominal width X for the font. Optional.
        /// </summary>
        public double? WidthDifferenceFromNominal { get; }

        /// <summary>
        /// Create a new <see cref="Type2Glyph"/>.
        /// </summary>
        public Type2Glyph(PdfPath path, double? widthDifferenceFromNominal)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            WidthDifferenceFromNominal = widthDifferenceFromNominal;
        }

        public double GetWidth(CompactFontFormatPrivateDictionary privateDictionary)
        {
            if (privateDictionary == null)
            {
                throw new ArgumentNullException(nameof(privateDictionary));
            }

            if (!WidthDifferenceFromNominal.HasValue)
            {
                return Path.GetBoundingRectangle().GetValueOrDefault().Width;
            }

            return (double)privateDictionary.NominalWidthX + WidthDifferenceFromNominal.Value;
        }
    }
}
