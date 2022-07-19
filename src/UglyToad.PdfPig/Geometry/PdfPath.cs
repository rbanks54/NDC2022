﻿// ReSharper disable ArrangeRedundantParentheses
namespace UglyToad.PdfPig.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Core;

    /// <summary>
    /// A path in a PDF document, used by glyphs and page content. Can contain multiple sub-paths.
    /// </summary>
    public class PdfPath
    {
        private readonly List<IPathCommand> commands = new List<IPathCommand>();

        /// <summary>
        /// The sequence of sub-paths which form this <see cref="PdfPath"/>.
        /// </summary>
        public IReadOnlyList<IPathCommand> Commands => commands;

        private PdfPoint? currentPosition;

        private readonly TransformationMatrix currentTransformationMatrix;

        /// <summary>
        /// Create a new <see cref="PdfPath"/>.
        /// </summary>
        /// <param name="transformationMatrix">The transformation to apply to all points in this path.</param>
        public PdfPath(TransformationMatrix transformationMatrix)
        {
            currentTransformationMatrix = transformationMatrix;
        }

        internal void MoveTo(double x, double y)
        {
            currentPosition = currentTransformationMatrix.Transform(new PdfPoint(x, y));
            commands.Add(new Move(currentPosition.Value));
        }

        internal void LineTo(double x, double y)
        {
            if (currentPosition.HasValue)
            {
                var to = currentTransformationMatrix.Transform(new PdfPoint(x, y));
                commands.Add(new Line(currentPosition.Value, to));
                currentPosition = to;
            }
            else
            {
                MoveTo(x, y);
            }
        }

        internal void QuadraticCurveTo(double x1, double y1, double x2, double y2) { }

        internal void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (currentPosition.HasValue)
            {
                var to = currentTransformationMatrix.Transform(new PdfPoint(x3, y3));
                commands.Add(new BezierCurve(currentPosition.Value,
                    currentTransformationMatrix.Transform(new PdfPoint(x1, y1)), currentTransformationMatrix.Transform(new PdfPoint(x2, y2)), to));
                currentPosition = to;
            }
            else
            {
                MoveTo(x3, y3);
            }
        }

        internal void SetWindingRuleMode(int windingRule) { }

        internal void ClosePath()
        {
            commands.Add(new Close());
        }

        internal PdfRectangle? GetBoundingRectangle()
        {
            if (commands.Count == 0)
            {
                return null;
            }

            var minX = double.MaxValue;
            var maxX = double.MinValue;

            var minY = double.MaxValue;
            var maxY = double.MinValue;

            foreach (var command in commands)
            {
                var rect = command.GetBoundingRectangle();
                if (rect == null)
                {
                    continue;
                }

                if (rect.Value.Left < minX)
                {
                    minX = rect.Value.Left;
                }

                if (rect.Value.Right > maxX)
                {
                    maxX = rect.Value.Right;
                }

                if (rect.Value.Bottom < minY)
                {
                    minY = rect.Value.Bottom;
                }

                if (rect.Value.Top > maxY)
                {
                    maxY = rect.Value.Top;
                }
            }

            return new PdfRectangle(minX, minY, maxX, maxY);
        }

        internal string ToSvg()
        {
            var builder = new StringBuilder();
            foreach (var pathCommand in commands)
            {
                pathCommand.WriteSvg(builder);
            }

            if (builder.Length == 0)
            {
                return string.Empty;
            }

            if (builder[builder.Length - 1] == ' ')
            {
                builder.Remove(builder.Length - 1, 1);
            }

            return builder.ToString();
        }

        internal string ToFullSvg()
        {
            string BboxToRect(PdfRectangle box, string stroke)
            {
                var overallBbox = $"<rect x='{box.Left}' y='{box.Bottom}' width='{box.Width}' height='{box.Height}' stroke-width='2' fill='none' stroke='{stroke}'></rect>";
                return overallBbox;
            }

            var glyph = ToSvg();
            var bbox = GetBoundingRectangle();
            var bboxes = new List<PdfRectangle>();

            foreach (var command in commands)
            {
                var segBbox = command.GetBoundingRectangle();
                if (segBbox.HasValue)
                {
                    bboxes.Add(segBbox.Value);
                }
            }

            var path = $"<path d='{glyph}' stroke='cyan' stroke-width='3'></path>";
            var bboxRect = bbox.HasValue ? BboxToRect(bbox.Value, "yellow") : string.Empty;
            var others = string.Join(" ", bboxes.Select(x => BboxToRect(x, "gray")));
            var result = $"<svg width='500' height='500'><g transform=\"scale(0.2, -0.2) translate(100, -700)\">{path} {bboxRect} {others}</g></svg>";

            return result;
        }

        /// <summary>
        /// A command in a <see cref="PdfPath"/>.
        /// </summary>
        public interface IPathCommand
        {
            /// <summary>
            /// Returns the smallest rectangle which contains the path region given by this command.
            /// </summary>
            /// <returns></returns>
            PdfRectangle? GetBoundingRectangle();

            /// <summary>
            /// Converts from the path command to an SVG string representing the path operation.
            /// </summary>
            void WriteSvg(StringBuilder builder);
        }

        /// <summary>
        /// Close the current <see cref="PdfPath"/>.
        /// </summary>
        public class Close : IPathCommand
        {
            /// <inheritdoc />
            public PdfRectangle? GetBoundingRectangle()
            {
                return null;
            }

            /// <inheritdoc />
            public void WriteSvg(StringBuilder builder)
            {
                builder.Append("Z ");
            }
        }

        /// <summary>
        /// Move drawing of the current <see cref="PdfPath"/> to the specified location.
        /// </summary>
        public class Move : IPathCommand
        {
            /// <summary>
            /// The location to move to.
            /// </summary>
            public PdfPoint Location { get; }

            /// <summary>
            /// Create a new <see cref="Move"/> path command.
            /// </summary>
            /// <param name="location"></param>
            public Move(PdfPoint location)
            {
                Location = location;
            }

            /// <summary>
            /// Returns <see langword="null"/> since this generates no visible path.
            /// </summary>
            public PdfRectangle? GetBoundingRectangle()
            {
                return null;
            }

            /// <inheritdoc />
            public void WriteSvg(StringBuilder builder)
            {
                builder.Append("M ").Append(Location.X).Append(' ').Append(Location.Y).Append(' ');
            }
        }

        /// <summary>
        /// Draw a straight line between two points.
        /// </summary>
        public class Line : IPathCommand
        {
            /// <summary>
            /// The start of the line.
            /// </summary>
            public PdfPoint From { get; }

            /// <summary>
            /// The end of the line.
            /// </summary>
            public PdfPoint To { get; }

            /// <summary>
            /// Create a new <see cref="Line"/>.
            /// </summary>
            public Line(PdfPoint from, PdfPoint to)
            {
                From = from;
                To = to;
            }

            /// <inheritdoc />
            public PdfRectangle? GetBoundingRectangle()
            {
                return new PdfRectangle(From, To);
            }

            /// <inheritdoc />
            public void WriteSvg(StringBuilder builder)
            {
                builder.AppendFormat("L {0} {1} ", To.X, To.Y);
            }
        }

        /// <summary>
        /// Draw a Bezier curve given by the start, control and end points.
        /// </summary>
        public class BezierCurve : IPathCommand
        {
            /// <summary>
            /// The start point of the Bezier curve.
            /// </summary>
            public PdfPoint StartPoint { get; }

            /// <summary>
            /// The first control point of the curve.
            /// </summary>
            public PdfPoint FirstControlPoint { get; }

            /// <summary>
            /// The second control point of the curve.
            /// </summary>
            public PdfPoint SecondControlPoint { get; }

            /// <summary>
            /// The end point of the curve.
            /// </summary>
            public PdfPoint EndPoint { get; }

            /// <summary>
            /// Create a Bezier curve at the provided points.
            /// </summary>
            public BezierCurve(PdfPoint startPoint, PdfPoint firstControlPoint, PdfPoint secondControlPoint, PdfPoint endPoint)
            {
                StartPoint = startPoint;
                FirstControlPoint = firstControlPoint;
                SecondControlPoint = secondControlPoint;
                EndPoint = endPoint;
            }

            /// <inheritdoc />
            public PdfRectangle? GetBoundingRectangle()
            {
                // Optimised
                double minX;
                double maxX;
                if (StartPoint.X <= EndPoint.X)
                {
                    minX = StartPoint.X;
                    maxX = EndPoint.X;
                }
                else
                {
                    minX = EndPoint.X;
                    maxX = StartPoint.X;
                }

                double minY;
                double maxY;
                if (StartPoint.Y <= EndPoint.Y)
                {
                    minY = StartPoint.Y;
                    maxY = EndPoint.Y;
                }
                else
                {
                    minY = EndPoint.Y;
                    maxY = StartPoint.Y;
                }

                if (TrySolveQuadratic(true, minX, maxX, out var xSolutions))
                {
                    minX = xSolutions.min;
                    maxX = xSolutions.max;
                }

                if (TrySolveQuadratic(false, minY, maxY, out var ySolutions))
                {
                    minY = ySolutions.min;
                    maxY = ySolutions.max;
                }

                return new PdfRectangle(minX, minY, maxX, maxY);
            }

            /// <inheritdoc />
            public void WriteSvg(StringBuilder builder)
            {
                builder.AppendFormat("C {0} {1}, {2} {3}, {4} {5} ", FirstControlPoint.X, FirstControlPoint.Y, SecondControlPoint.X, SecondControlPoint.Y,
                    EndPoint.X, EndPoint.Y);
            }


            private bool TrySolveQuadratic(bool isX, double currentMin, double currentMax, out (double min, double max) solutions)
            {
                solutions = default((double, double));

                // This method has been optimised for performance by eliminating calls to Math.

                // Given k points the general form is:
                // P = (1-t)^(k - i - 1)*t^(i)*P_i
                // 
                // For 4 points this gives:
                // P = (1−t)^3*P_1 + 3(1−t)^2*t*P_2 + 3(1−t)*t^2*P_3 + t^3*P_4
                // The differential is:
                // P' = 3(1-t)^2(P_2 - P_1) + 6(1-t)^t(P_3 - P_2) + 3t^2(P_4 - P_3)

                // P' = 3da(1-t)^2 + 6db(1-t)t + 3dct^2
                // P' = 3da - 3dat - 3dat + 3dat^2 + 6dbt - 6dbt^2 + 3dct^2
                // P' = (3da - 6db + 3dc)t^2 + (6db - 3da - 3da)t + 3da
                var p1 = (isX ? StartPoint.X : StartPoint.Y);
                var p2 = (isX ? FirstControlPoint.X : FirstControlPoint.Y);
                var p3 = (isX ? SecondControlPoint.X : SecondControlPoint.Y);
                var p4 = (isX ? EndPoint.X : EndPoint.Y);

                var threeda = 3 * (p2 - p1);
                var sixdb = 6 * (p3 - p2);
                var threedc = 3 * (p4 - p3);

                var a = threeda - sixdb + threedc;
                var b = sixdb - threeda - threeda;
                var c = threeda;

                // P' = at^2 + bt + c
                // t = (-b (+/-) sqrt(b ^ 2 - 4ac))/2a

                var sqrtable = (b * b) - (4 * a * c);

                if (sqrtable < 0)
                {
                    return false;
                }

                var sqrt = Math.Sqrt(sqrtable);
                var divisor = 2 * a;

                var t1 = (-b + sqrt) / divisor;
                var t2 = (-b - sqrt) / divisor;

                if (t1 >= 0 && t1 <= 1)
                {
                    var sol1 = ValueWithT(p1, p2, p3, p4, t1);
                    if (sol1 < currentMin)
                    {
                        currentMin = sol1;
                    }

                    if (sol1 > currentMax)
                    {
                        currentMax = sol1;
                    }
                }

                if (t2 >= 0 && t2 <= 1)
                {
                    var sol2 = ValueWithT(p1, p2, p3, p4, t2);
                    if (sol2 < currentMin)
                    {
                        currentMin = sol2;
                    }

                    if (sol2 > currentMax)
                    {
                        currentMax = sol2;
                    }
                }

                solutions = (currentMin, currentMax);

                return true;
            }

            private static double ValueWithT(double p1, double p2, double p3, double p4, double t)
            {
                // P = (1−t)^3*P_1 + 3(1−t)^2*t*P_2 + 3(1−t)*t^2*P_3 + t^3*P_4
                var oneMinusT = 1 - t;
                var p = ((oneMinusT * oneMinusT * oneMinusT) * p1)
                        + (3 * (oneMinusT * oneMinusT) * t * p2)
                        + (3 * oneMinusT * (t * t) * p3)
                        + ((t * t * t) * p4);

                return p;
            }
        }

        internal void Rectangle(double x, double y, double width, double height)
        {
            currentPosition = currentTransformationMatrix.Transform(new PdfPoint(x, y));
            LineTo(x + width, y);
            LineTo(x + width, y + height);
            LineTo(x, y + height);
            LineTo(x, y);
        }
    }
}
