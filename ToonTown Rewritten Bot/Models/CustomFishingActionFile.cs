using System;
using System.Collections.Generic;

namespace ToonTown_Rewritten_Bot.Models
{
    /// <summary>
    /// Represents the v2 custom fishing action file format with embedded calibration data.
    /// </summary>
    public class CustomFishingActionFile
    {
        /// <summary>
        /// File format version. v1 was just an array of FishingActionCommand, v2 adds calibration.
        /// </summary>
        public int Version { get; set; } = 2;

        /// <summary>
        /// Friendly name for this action file (e.g., "Estate Left Dock").
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Optional description of this fishing spot.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Embedded calibration settings for this location.
        /// If null, falls back to global settings.
        /// </summary>
        public CalibrationData Calibration { get; set; }

        /// <summary>
        /// The list of actions to execute (walk path, sell fish, etc.).
        /// </summary>
        public List<FishingActionCommand> Actions { get; set; } = new List<FishingActionCommand>();

        /// <summary>
        /// When this file was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// When this file was last modified.
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Calibration data embedded in action files for location-specific fish detection.
    /// </summary>
    public class CalibrationData
    {
        /// <summary>
        /// Scan area settings (percentage-based for resolution independence).
        /// </summary>
        public ScanAreaCalibration ScanArea { get; set; }

        /// <summary>
        /// Pond color settings for fish shadow detection.
        /// </summary>
        public PondColorCalibration PondColors { get; set; }
    }

    /// <summary>
    /// Scan area calibration data stored as percentages of window size.
    /// </summary>
    public class ScanAreaCalibration
    {
        public float XPercent { get; set; }
        public float YPercent { get; set; }
        public float WidthPercent { get; set; }
        public float HeightPercent { get; set; }
    }

    /// <summary>
    /// Pond color calibration data for fish shadow detection.
    /// </summary>
    public class PondColorCalibration
    {
        // Water color RGB
        public int WaterR { get; set; }
        public int WaterG { get; set; }
        public int WaterB { get; set; }

        // Fish shadow color RGB
        public int ShadowR { get; set; }
        public int ShadowG { get; set; }
        public int ShadowB { get; set; }

        // Color tolerance for matching
        public int ToleranceR { get; set; } = 15;
        public int ToleranceG { get; set; } = 15;
        public int ToleranceB { get; set; } = 15;
    }
}
