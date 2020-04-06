﻿using System.Text.Json.Serialization;

/// <summary>
/// Scribble.rs ♯ data namespace
/// </summary>
namespace ScribblersSharp.Data
{
    /// <summary>
    /// Game message data class
    /// </summary>
    /// <typeparam name="T">Game message data type</typeparam>
    internal class GameMessageData<T> : BaseGameMessageData
    {
        /// <summary>
        /// Game message data
        /// </summary>
        [JsonPropertyName("data")]
        public T Data { get; set; }
    }
}
