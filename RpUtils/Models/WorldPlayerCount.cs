using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Gets or sets the currently active roleplay count.
        /// </summary>
        public int Count { get; set; }

    }
}
