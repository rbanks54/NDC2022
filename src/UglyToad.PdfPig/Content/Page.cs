﻿namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Annotations;
    using Graphics.Operations;
    using Tokens;
    using Util;
    using Util.JetBrains.Annotations;
    using XObjects;
    using Geometry;

    /// <summary>
    /// Contains the content and provides access to methods of a single page in the <see cref="PdfDocument"/>.
    /// </summary>
    public class Page
    {
        /// <summary>
        /// The raw PDF dictionary token for this page in the document.
        /// </summary>
        public DictionaryToken Dictionary { get; }

        /// <summary>
        /// The page number (starting at 1).
        /// </summary>
        public int Number { get; }

        internal MediaBox MediaBox { get; }

        internal CropBox CropBox { get; }

        internal PageContent Content { get; }

        /// <summary>
        /// The rotation of the page in degrees (clockwise). Valid values are 0, 90, 180 and 270.
        /// </summary>
        public PageRotationDegrees Rotation { get; }

        /// <summary>
        /// The set of <see cref="Letter"/>s drawn by the PDF content.
        /// </summary>
        public IReadOnlyList<Letter> Letters => Content?.Letters ?? new Letter[0];
        
        /// <summary>
        /// The full text of all characters on the page in the order they are presented in the PDF content.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the width of the page in points.
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// Gets the height of the page in points.
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// The size of the page according to the standard page sizes or Custom if no matching standard size found.
        /// </summary>
        public PageSize Size { get; }

        /// <summary>
        /// The parsed graphics state operations in the content stream for this page. 
        /// </summary>
        public IReadOnlyList<IGraphicsStateOperation> Operations => Content.GraphicsStateOperations;

        /// <summary>
        /// Access to members whose future locations within the API will change without warning.
        /// </summary>
        [NotNull]
        public Experimental ExperimentalAccess { get; }

        internal Page(int number, DictionaryToken dictionary, MediaBox mediaBox, CropBox cropBox, PageRotationDegrees rotation, PageContent content,
            AnnotationProvider annotationProvider)
        {
            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "Page number cannot be 0 or negative.");
            }

            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

            Number = number;
            MediaBox = mediaBox;
            CropBox = cropBox;
            Rotation = rotation;
            Content = content;
            Text = GetText(content);

            Width = mediaBox.Bounds.Width;
            Height = mediaBox.Bounds.Height;

            Size = mediaBox.Bounds.GetPageSize();
            ExperimentalAccess = new Experimental(this, annotationProvider);
        }

        private static string GetText(PageContent content)
        {
            if (content?.Letters == null)
            {
                return string.Empty;
            }

            return string.Join(string.Empty, content.Letters.Select(x => x.Value));
        }

        /// <summary>
        /// Use the default <see cref="IWordExtractor"/> to get the words for this page.
        /// </summary>
        /// <returns>The words on this page.</returns>
        public IEnumerable<Word> GetWords() => GetWords(DefaultWordExtractor.Instance);

        /// <summary>
        /// Use a custom <see cref="IWordExtractor"/> to get the words for this page.
        /// </summary>
        /// <param name="wordExtractor">The word extractor to use to generate words.</param>
        /// <returns>The words on this page.</returns>
        public IEnumerable<Word> GetWords(IWordExtractor wordExtractor)
        {
            return (wordExtractor ?? DefaultWordExtractor.Instance).GetWords(Letters);
        }

        /// <summary>
        /// Provides access to useful members which will change in future releases.
        /// </summary>
        public class Experimental
        {
            private readonly Page page;
            private readonly AnnotationProvider annotationProvider;

            /// <summary>
            /// The set of <see cref="PdfPath"/>s drawn by the PDF content.
            /// </summary>
            public IReadOnlyList<PdfPath> Paths => page.Content?.Paths ?? new List<PdfPath>();

            internal Experimental(Page page, AnnotationProvider annotationProvider)
            {
                this.page = page;
                this.annotationProvider = annotationProvider;
            }

            /// <summary>
            /// Retrieve any images referenced in this page's content.
            /// These are returned as <see cref="XObjectImage"/>s which are 
            /// raw data from the PDF's content rather than images.
            /// </summary>
            public IEnumerable<XObjectImage> GetRawImages()
            {
                return page.Content.GetImages();
            }

            /// <summary>
            /// Get the annotation objects from the page.
            /// </summary>
            /// <returns>The lazily evaluated set of annotations on this page.</returns>
            public IEnumerable<Annotation> GetAnnotations()
            {
                return annotationProvider.GetAnnotations();
            }

            /// <summary>
            /// Gets the calculated letter size in points.
            /// This is considered experimental because the calculated value is incorrect for some documents at present.
            /// </summary>
            public double GetPointSize(Letter letter)
            {
                return letter.PointSize;
            }
        }
    }
}
