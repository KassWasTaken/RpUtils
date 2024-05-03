namespace RpUtils.Models
{
    /// <summary>
    /// Information about a locations in the game world players are roleplaying in.
    /// </summary>
    internal class WorldPlayerCount
    {
        /// <summary>
        /// Gets or sets the world name.
        /// </summary>
        public string WorldName { get; set; }

        /// <summary>
        /// Gets or sets the location within the world.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the map sublocation within the world.
        /// </summary>
        public string Sublocation { get; set; }

        /// <summary>
        /// Gets or sets the currently active roleplay count.
        /// </summary>
        public int Count { get; set; }
    }
}
