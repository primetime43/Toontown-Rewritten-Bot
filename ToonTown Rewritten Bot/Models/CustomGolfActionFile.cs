using System;
using System.Collections.Generic;

namespace ToonTown_Rewritten_Bot.Models
{
    /// <summary>
    /// Represents the v2 custom golf action file format with metadata.
    /// </summary>
    public class CustomGolfActionFile
    {
        /// <summary>
        /// File format version. v1 was just an array of GolfActionCommand, v2 adds metadata.
        /// </summary>
        public int Version { get; set; } = 2;

        /// <summary>
        /// Friendly name for this action file (e.g., "Afternoon Tee Easy Hole 1").
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Optional description of this golf shot.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Difficulty level (EASY, MEDIUM, HARD).
        /// </summary>
        public string Difficulty { get; set; } = "";

        /// <summary>
        /// Course name this action is for.
        /// </summary>
        public string CourseName { get; set; } = "";

        /// <summary>
        /// Hole number (1, 2, or 3).
        /// </summary>
        public int HoleNumber { get; set; } = 0;

        /// <summary>
        /// The list of actions to execute.
        /// </summary>
        public List<GolfActionCommand> Actions { get; set; } = new List<GolfActionCommand>();

        /// <summary>
        /// When this file was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// When this file was last modified.
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }
}
