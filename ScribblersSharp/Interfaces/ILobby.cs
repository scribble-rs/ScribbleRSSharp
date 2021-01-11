﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.WebSockets;
using System.Threading.Tasks;

/// <summary>
/// Scribble.rs ♯ namespace
/// </summary>
namespace ScribblersSharp
{
    /// <summary>
    /// Lobby interface
    /// </summary>
    public interface ILobby : IDisposable
    {
        /// <summary>
        /// "ready" game message received event
        /// </summary>
        event ReadyGameMessageReceivedDelegate OnReadyGameMessageReceived;

        /// <summary>
        /// "next-turn" game message received event
        /// </summary>
        event NextTurnGameMessageReceivedDelegate OnNextTurnGameMessageReceived;

        /// <summary>
        /// "update-players" game message received event
        /// </summary>
        event UpdatePlayersGameMessageReceivedDelegate OnUpdatePlayersGameMessageReceived;

        /// <summary>
        /// "update-wordhint" game message received event
        /// </summary>
        event UpdateWordhintGameMessageReceivedDelegate OnUpdateWordhintGameMessageReceived;

        /// <summary>
        /// "message" game message received event
        /// </summary>
        event MessageGameMessageReceivedDelegate OnMessageGameMessageReceived;

        /// <summary>
        /// "non-guessing-player-message" game message received event
        /// </summary>
        event NonGuessingPlayerMessageGameMessageReceivedDelegate OnNonGuessingPlayerMessageGameMessageReceived;

        /// <summary>
        /// "system-message" game message received event
        /// </summary>
        event SystemMessageGameMessageReceivedDelegate OnSystemMessageGameMessageReceived;

        /// <summary>
        /// "line" game message received event
        /// </summary>
        event LineGameMessageReceivedDelegate OnLineGameMessageReceived;

        /// <summary>
        /// "fill" game message received event
        /// </summary>
        event FillGameMessageReceivedDelegate OnFillGameMessageReceived;

        /// <summary>
        /// "clear-drawing-board" game message received event
        /// </summary>
        event ClearDrawingBoardGameMessageReceivedDelegate OnClearDrawingBoardGameMessageReceived;

        /// <summary>
        /// "your-turn" game message received event
        /// </summary>
        event YourTurnGameMessageReceivedDelegate OnYourTurnGameMessageReceived;

        /// <summary>
        /// "correct-guess" game message received event
        /// </summary>
        event CorrectGuessGameMessageReceivedDelegate OnCorrectGuessGameMessageReceived;

        /// <summary>
        /// WebSocket state
        /// </summary>
        WebSocketState WebSocketState { get; }

        /// <summary>
        /// Lobby ID
        /// </summary>
        string LobbyID { get; }

        /// <summary>
        /// Username
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Drawing board base width
        /// </summary>
        uint DrawingBoardBaseWidth { get; }

        /// <summary>
        /// Drawing board base height
        /// </summary>
        uint DrawingBoardBaseHeight { get; }

        /// <summary>
        /// Player ID
        /// </summary>
        string PlayerID { get; }

        /// <summary>
        /// Is player drawing
        /// </summary>
        bool IsPlayerDrawing { get; }

        /// <summary>
        /// Round
        /// </summary>
        uint Round { get; }

        /// <summary>
        /// Maximal rounds
        /// </summary>
        uint MaximalRounds { get; }

        /// <summary>
        /// Round end time
        /// </summary>
        long RoundEndTime { get; }

        /// <summary>
        /// Word hints
        /// </summary>
        IReadOnlyList<IWordHint> WordHints { get; }

        /// <summary>
        /// Players
        /// </summary>
        IReadOnlyList<IPlayer> Players { get; }

        /// <summary>
        /// Adds a game message parser
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="onGameMessageParsed">On message parsed</param>
        /// <param name="onGameMessageParseFailed">On message parse failed</param>
        /// <returns>Message parser</returns>
        IGameMessageParser<T> AddMessageParser<T>(GameMessageParsedDelegate<T> onGameMessageParsed, GameMessageParseFailedDelegate<T> onGameMessageParseFailed = null) where T : IReceiveGameMessageData;

        /// <summary>
        /// Removes the specified game message parser
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="gameMessageParser">Message parser</param>
        /// <returns>"true" if message parser was successfully removed, otherwise "false"</returns>
        bool RemoveMessageParser<T>(IGameMessageParser<T> gameMessageParser) where T : IReceiveGameMessageData;

        /// <summary>
        /// Parses incoming message
        /// </summary>
        /// <param name="json">JSON</param>
        void ParseMessage(string json);

        /// <summary>
        /// Send start game (asynchronous)
        /// </summary>
        /// <returns>Task</returns>
        Task SendStartGameAsync();

        /// <summary>
        /// Clear drawing board (asynchronous)
        /// </summary>
        /// <returns>Task</returns>
        Task SendClearDrawingBoardAsync();

        /// <summary>
        /// Send draw command (asynchronous)
        /// </summary>
        /// <param name="type">Draw command type</param>
        /// <param name="from">Draw from</param>
        /// <param name="to">Draw to</param>
        /// <param name="color">Draw color</param>
        /// <param name="lineWidth">Line width</param>
        /// <returns>Task</returns>
        Task SendDrawCommandAsync(EDrawCommandType type, float fromX, float fromY, float toX, float toY, Color color, float lineWidth);

        /// <summary>
        /// Send choose word (asynchronous)
        /// </summary>
        /// <param name="index">Choose word index</param>
        /// <returns>Task</returns>
        Task SendChooseWordAsync(uint index);

        /// <summary>
        /// Send chat message (asynchronous)
        /// </summary>
        /// <param name="content">Content</param>
        /// <returns>Task</returns>
        Task SendChatMessageAsync(string content);

        /// <summary>
        /// Process events synchronously
        /// </summary>
        void ProcessEvents();

        /// <summary>
        /// Close (asynchronous)
        /// </summary>
        void CloseAsync();
    }
}
