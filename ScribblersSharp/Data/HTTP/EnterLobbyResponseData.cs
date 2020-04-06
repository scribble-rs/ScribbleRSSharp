﻿using System.Text.Json.Serialization;

/// <summary>
/// Scribble.rs ♯ data namespace
/// </summary>
namespace ScribblersSharp.Data
{
    /// <summary>
    /// ENter lobby response data class
    /// </summary>
    internal class EnterLobbyResponseData : IResponseData
    {
        /// <summary>
        /// Lobby ID
        /// </summary>
        [JsonPropertyName("lobbyId")]
        public string LobbyID { get; set; }

        /// <summary>
        /// Drawing board base width
        /// </summary>
        [JsonPropertyName("drawingBoardBaseWidth")]
        public uint DrawingBoardBaseWidth { get; set; }

        /// <summary>
        /// Drawing board base height
        /// </summary>
        [JsonPropertyName("drawingBoardBaseHeight")]
        public uint DrawingBoardBaseHeight { get; set; }
    }
}
