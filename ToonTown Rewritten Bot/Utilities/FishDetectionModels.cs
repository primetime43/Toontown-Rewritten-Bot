using System;
using System.Collections.Generic;
using System.Drawing;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// A single fish shadow candidate for calibration.
    /// </summary>
    public class FishCandidate
    {
        public Point Position { get; set; }
        public Color Color { get; set; }
        public int Size { get; set; }
        public bool HasBubblesAbove { get; set; }
        public double DistanceFromCenter { get; set; }
        public double CastPower { get; set; }  // How much drag power needed (lower = closer/easier)
    }

    /// <summary>
    /// Result of fish detection for debugging/visualization purposes.
    /// </summary>
    public class FishDetectionDebugResult
    {
        public Rectangle ScanArea { get; set; }
        public bool UsedDynamicPondDetection { get; set; }
        public Color TargetBubbleColor { get; set; }
        public Tolerance ColorTolerance { get; set; }
        public int AvgBrightness { get; set; }
        public int DarkThreshold { get; set; }
        public int DarkPixelCount { get; set; }
        public List<List<Point>> Blobs { get; set; } = new List<List<Point>>();
        public int RejectedBlobCount { get; set; }
        public Point? BestShadowPosition { get; set; }
        public Color BestShadowColor { get; set; }
        public bool HasBubblesAbove { get; set; }
        public int CandidateCount { get; set; }
        public int CandidatesWithBubbles { get; set; }
        public Point? RodButtonPosition { get; set; }
        public Point? CastDestination { get; set; }
        public bool UsingLearnedColor { get; set; }
        public Color? LearnedColor { get; set; }
        public bool NeedsCalibration { get; set; }
        public List<FishCandidate> AllCandidates { get; set; } = new List<FishCandidate>();
    }

    /// <summary>
    /// Configuration for a specific fishing spot.
    /// </summary>
    public class FishingSpotConfig
    {
        public Rectangle ScanArea { get; }
        public Color BubbleColor { get; }
        public Tolerance ColorTolerance { get; }
        public int YAdjustment { get; }

        public FishingSpotConfig(Rectangle scanArea, Color bubbleColor, Tolerance tolerance, int yAdjustment)
        {
            ScanArea = scanArea;
            BubbleColor = bubbleColor;
            ColorTolerance = tolerance;
            YAdjustment = yAdjustment;
        }
    }

    /// <summary>
    /// RGB tolerance values for color matching.
    /// </summary>
    public struct Tolerance
    {
        public int R { get; }
        public int G { get; }
        public int B { get; }

        public Tolerance(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

    /// <summary>
    /// Result of fish detection and casting calculation.
    /// </summary>
    public class CastingResult
    {
        public Point BubblePosition { get; set; }
        public Point RodButtonPosition { get; set; }
        public Point CastDestination { get; set; }
    }
}
