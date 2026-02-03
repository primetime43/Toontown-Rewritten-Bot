using System;
using System.Collections.Generic;

namespace ToonTown_Rewritten_Bot.Models
{
    /// <summary>
    /// Represents a custom gardening action file with metadata (v2 format).
    /// </summary>
    public class CustomGardeningActionFile
    {
        /// <summary>
        /// File format version.
        /// </summary>
        public int Version { get; set; } = 2;

        /// <summary>
        /// Display name of this action file.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Description of what this routine does.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Location in the estate (e.g., "Front Yard", "Back Yard", "Left Side").
        /// </summary>
        public string Location { get; set; } = "";

        /// <summary>
        /// Number of flower beds this routine covers.
        /// </summary>
        public int FlowerBedCount { get; set; } = 0;

        /// <summary>
        /// The list of gardening actions in this file.
        /// </summary>
        public List<GardeningActionCommand> Actions { get; set; } = new List<GardeningActionCommand>();

        /// <summary>
        /// When the file was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// When the file was last modified.
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }
}
