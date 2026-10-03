using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace ToonTown_Rewritten_Bot.Utilities
{
    internal static class LocalContrastShadowDetector
    {
        internal static bool ShouldUse(Color water, Color shadow)
        {
            double difference = (water.R + water.G + water.B - shadow.R - shadow.G - shadow.B) / 3.0;
            return difference > 0 && difference <= 16;
        }

        internal static List<Point> FindPixels(Bitmap frame, Rectangle scanArea, int step,
            Color calibratedWater, CancellationToken cancellationToken)
        {
            var area = Rectangle.Intersect(scanArea, new Rectangle(Point.Empty, frame.Size));
            var points = new List<Point>();
            if (area.IsEmpty) return points;
            int columns = (area.Width + step - 1) / step;
            int rows = (area.Height + step - 1) / step;
            var colors = new Color[columns * rows];
            var light = new double[colors.Length];
            var water = new bool[colors.Length];
            // Sample once; neighboring comparisons then use arrays rather than repeated
            // bitmap reads. Compare within the scan area so docks outside it cannot help
            // a patch pass the surrounding-water check.
            for (int y = 0; y < rows; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int x = 0; x < columns; x++)
                {
                    int index = y * columns + x;
                    var color = frame.GetPixel(area.X + x * step, area.Y + y * step);
                    colors[index] = color;
                    light[index] = (color.R + color.G + color.B) / 3.0;
                    double calibratedLight = (calibratedWater.R + calibratedWater.G + calibratedWater.B) / 3.0;
                    // Use the sampled pond's tint, including gray or dark-blue ponds,
                    // while allowing the brightness to vary across its surface.
                    water[index] = light[index] >= 10 && light[index] <= 230 &&
                        Math.Abs(light[index] - calibratedLight) <= 45 &&
                        Math.Abs((color.G - color.R) - (calibratedWater.G - calibratedWater.R)) <= 12 &&
                        Math.Abs((color.B - color.G) - (calibratedWater.B - calibratedWater.G)) <= 12;
                }
            }

            int radiusX = Math.Max(2, (int)Math.Round(45 * frame.Width / 1600.0 / step));
            int radiusY = Math.Max(2, (int)Math.Round(35 * frame.Height / 1151.0 / step));
            Point[] Ring(int rx, int ry)
            {
                int dx = Math.Max(1, rx * 3 / 4), dy = Math.Max(1, ry * 3 / 4);
                return new[] { new Point(-rx, 0), new Point(rx, 0), new Point(0, -ry), new Point(0, ry),
                    new Point(-dx, -dy), new Point(dx, -dy), new Point(-dx, dy), new Point(dx, dy) };
            }
            // A larger ring also handles broad shadows whose edges extend beyond
            // the smaller ring. Each still has to be surrounded in most directions.
            var rings = new[] { Ring(radiusX, radiusY), Ring(radiusX * 2, radiusY * 2) };
            for (int y = radiusY; y < rows - radiusY; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int x = radiusX; x < columns - radiusX; x++)
                {
                    int index = y * columns + x;
                    if (!water[index]) continue;
                    foreach (var ring in rings)
                    {
                        int rx = ring[1].X, ry = ring[3].Y;
                        if (x < rx || x >= columns - rx || y < ry || y >= rows - ry) continue;
                        int lighterNeighbors = 0;
                        foreach (var offset in ring)
                        {
                            int neighbor = (y + offset.Y) * columns + x + offset.X;
                            double contrast = light[neighbor] - light[index];
                            // Most directions must contain slightly lighter water. A smooth
                            // lighting gradient, bright bubble or UI edge alone is insufficient.
                            if (water[neighbor] && contrast >= 3 && contrast <= 24 &&
                                Math.Abs((colors[neighbor].G - colors[neighbor].R) - (colors[index].G - colors[index].R)) <= 10 &&
                                Math.Abs((colors[neighbor].B - colors[neighbor].G) - (colors[index].B - colors[index].G)) <= 10)
                                lighterNeighbors++;
                        }
                        if (lighterNeighbors >= 6)
                        {
                            points.Add(new Point(area.X + x * step, area.Y + y * step));
                            break;
                        }
                    }
                }
            }
            return points;
        }
    }
}
