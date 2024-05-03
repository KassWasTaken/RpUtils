namespace RpUtils.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Play counts information for the "Roleplaying Now" display.
    /// </summary>
    internal class PlayerCountNode
    {
        /// <summary>
        /// Gets or sets the location within the world.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the currently active roleplay count.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets the list of sub locations.
        /// </summary>
        public IList<PlayerCountNode> SubLocations { get; set;} = new List<PlayerCountNode>();
    }
}
