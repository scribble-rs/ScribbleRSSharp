﻿using System.Text.Json.Serialization;

/// <summary>
/// Scribble.rs ♯ data namespace
/// </summary>
namespace ScribblersSharp.Data
{
    /// <summary>
    /// Next turn data class
    /// </summary>
    internal class NextTurnData
    {
        /// <summary>
        /// Round end time
        /// </summary>
        [JsonPropertyName("roundEndTime")]
        public ulong RoundEndTime { get; set; }

        /// <summary>
        /// Players
        /// </summary>
        [JsonPropertyName("players")]
        public PlayerData[] Players { get; set; }

        /// <summary>
        /// Round
        /// </summary>
        [JsonPropertyName("round")]
        public uint Round { get; set; }
    }
}
