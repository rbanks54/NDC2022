namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Geometry;

    /// <inheritdoc />
    /// <summary>
    /// Begin a new subpath by moving the current point to coordinates (x, y), omitting any connecting line segment.
    /// </summary>
    public class BeginNewSubpath : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "m";
        public decimal X { get; }
        public decimal Y { get; }

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The point to begin the new subpath at.
        /// </summary>
        public PdfPoint Point { get; }

        /// <summary>
        /// Create a new <see cref="BeginNewSubpath"/>.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        public BeginNewSubpath(decimal x, decimal y)
        {
            X = x;
            Y = y;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var point = new PdfPoint(X, Y);
            operationContext.BeginSubpath();
            var pointTransform = operationContext.CurrentTransformationMatrix.Transform(point);
            operationContext.CurrentPosition = pointTransform;
            operationContext.CurrentPath.MoveTo(pointTransform.X, pointTransform.Y);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Y);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{X} {Y} {Symbol}";
        }
    }
}