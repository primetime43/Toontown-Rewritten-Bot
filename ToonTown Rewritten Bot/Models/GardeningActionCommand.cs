namespace ToonTown_Rewritten_Bot.Models
{
    /// <summary>
    /// Represents a single gardening action command.
    /// </summary>
    public class GardeningActionCommand
    {
        /// <summary>
        /// The action type (e.g., "WALK FORWARD", "PLANT FLOWER", "WATER PLANT").
        /// </summary>
        public string Action { get; set; } = "";

        /// <summary>
        /// Duration in milliseconds for movement actions.
        /// </summary>
        public int Duration { get; set; } = 0;

        /// <summary>
        /// For PLANT FLOWER: the flower name.
        /// </summary>
        public string FlowerName { get; set; } = "";

        /// <summary>
        /// For PLANT FLOWER: the bean sequence (e.g., "rgo" for red-green-orange).
        /// </summary>
        public string BeanSequence { get; set; } = "";

        /// <summary>
        /// For WATER PLANT: the number of times to water.
        /// </summary>
        public int WaterCount { get; set; } = 0;
    }
}
