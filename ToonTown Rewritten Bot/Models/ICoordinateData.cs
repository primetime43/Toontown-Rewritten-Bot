namespace ToonTown_Rewritten_Bot.Models
{
    /// <summary>
    /// Provides a contract for coordinate data, including keys, descriptions, and positions.
    /// </summary>
    public interface ICoordinateData
    {
        /// <summary>
        /// Gets or sets the key associated with the coordinate data.
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// Gets or sets the description of the coordinate data.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets or sets the X-coordinate.
        /// </summary>
        int X { get; set; }

        /// <summary>
        /// Gets or sets the Y-coordinate.
        /// </summary>
        int Y { get; set; }
    }
}