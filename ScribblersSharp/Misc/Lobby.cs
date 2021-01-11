﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScribblersSharp.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Scribble.rs ♯ namespace
/// </summary>
namespace ScribblersSharp
{
    /// <summary>
    /// Lobby class
    /// </summary>
    internal class Lobby : ILobby
    {
        /// <summary>
        /// Available game message parsers
        /// </summary>
        private readonly Dictionary<string, List<IBaseGameMessageParser>> gameMessageParsers = new Dictionary<string, List<IBaseGameMessageParser>>();

        /// <summary>
        /// WebSocket receive thread
        /// </summary>
        private Thread webSocketReceiveThread;

        /// <summary>
        /// Received game messages
        /// </summary>
        private readonly ConcurrentQueue<string> receivedGameMessages = new ConcurrentQueue<string>();

        /// <summary>
        /// Client web socket
        /// </summary>
        private readonly ClientWebSocket clientWebSocket = new ClientWebSocket();

        /// <summary>
        /// Players
        /// </summary>
        private IPlayer[] players = Array.Empty<IPlayer>();

        /// <summary>
        /// Word hints
        /// </summary>
        private IWordHint[] wordHints = Array.Empty<IWordHint>();

        /// <summary>
        /// Receive buffer
        /// </summary>
        private ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(new byte[2048]);

        /// <summary>
        /// "ready" game message received event
        /// </summary>
        public event ReadyGameMessageReceivedDelegate OnReadyGameMessageReceived;

        /// <summary>
        /// "next-turn" game message received event
        /// </summary>
        public event NextTurnGameMessageReceivedDelegate OnNextTurnGameMessageReceived;

        /// <summary>
        /// "update-players" game message received event
        /// </summary>
        public event UpdatePlayersGameMessageReceivedDelegate OnUpdatePlayersGameMessageReceived;

        /// <summary>
        /// "update-wordhint" game message received event
        /// </summary>
        public event UpdateWordhintGameMessageReceivedDelegate OnUpdateWordhintGameMessageReceived;

        /// <summary>
        /// "message" game message received event
        /// </summary>
        public event MessageGameMessageReceivedDelegate OnMessageGameMessageReceived;

        /// <summary>
        /// "non-guessing-player-message" game message received event
        /// </summary>
        public event NonGuessingPlayerMessageGameMessageReceivedDelegate OnNonGuessingPlayerMessageGameMessageReceived;

        /// <summary>
        /// "system-message" game message received event
        /// </summary>
        public event SystemMessageGameMessageReceivedDelegate OnSystemMessageGameMessageReceived;

        /// <summary>
        /// "line" game message received event
        /// </summary>
        public event LineGameMessageReceivedDelegate OnLineGameMessageReceived;

        /// <summary>
        /// "fill" game message received event
        /// </summary>
        public event FillGameMessageReceivedDelegate OnFillGameMessageReceived;

        /// <summary>
        /// "clear-drawing-board" game message received event
        /// </summary>
        public event ClearDrawingBoardGameMessageReceivedDelegate OnClearDrawingBoardGameMessageReceived;

        /// <summary>
        /// "your-turn" game message received event
        /// </summary>
        public event YourTurnGameMessageReceivedDelegate OnYourTurnGameMessageReceived;

        /// <summary>
        /// "correct-guess" game message received event
        /// </summary>
        public event CorrectGuessGameMessageReceivedDelegate OnCorrectGuessGameMessageReceived;

        /// <summary>
        /// "drawing" game message received event
        /// </summary>
        public event DrawingGameMessageReceivedDelegate OnDrawingGameMessageReceived;

        /// <summary>
        /// This event will be invoked when a non-meaningful game message has been received.
        /// </summary>
        public event UnknownGameMessageReceivedDelegate OnUnknownGameMessageReceived;

        /// <summary>
        /// WebSocket state
        /// </summary>
        public WebSocketState WebSocketState => clientWebSocket.State;

        /// <summary>
        /// Lobby ID
        /// </summary>
        public string LobbyID { get; private set; }

        /// <summary>
        /// Username
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Drawing board base width
        /// </summary>
        public uint DrawingBoardBaseWidth { get; private set; }

        /// <summary>
        /// Drawing board base height
        /// </summary>
        public uint DrawingBoardBaseHeight { get; private set; }

        /// <summary>
        /// Player ID
        /// </summary>
        public string PlayerID { get; private set; } = string.Empty;

        /// <summary>
        /// Is player allowed to draw
        /// </summary>
        public bool IsPlayerAllowedToDraw { get; private set; }

        /// <summary>
        /// Round
        /// </summary>
        public uint Round { get; private set; }

        /// <summary>
        /// Maximal rounds
        /// </summary>
        public uint MaximalRounds { get; private set; }

        /// <summary>
        /// Round end time
        /// </summary>
        public long RoundEndTime { get; private set; }

        /// <summary>
        /// Word hints
        /// </summary>
        public IReadOnlyList<IWordHint> WordHints => wordHints;

        /// <summary>
        /// Players
        /// </summary>
        public IReadOnlyList<IPlayer> Players => players;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientWebSocket">Client WebSocket</param>
        /// <param name="username">Username</param>
        /// <param name="lobbyID">LobbyID</param>
        public Lobby(ClientWebSocket clientWebSocket, string username, string lobbyID, uint drawingBoardBaseWidth, uint drawingBoardBaseHeight)
        {
            this.clientWebSocket = clientWebSocket ?? throw new ArgumentNullException(nameof(clientWebSocket));
            Username = username ?? throw new ArgumentNullException(nameof(username));
            LobbyID = lobbyID ?? throw new ArgumentNullException(nameof(lobbyID));
            DrawingBoardBaseWidth = drawingBoardBaseWidth;
            DrawingBoardBaseHeight = drawingBoardBaseHeight;
            AddMessageParser<ReadyReceiveGameMessageData>((gameMessage, json) =>
            {
                ReadyData ready_data = gameMessage.Data;
                PlayerID = ready_data.PlayerID;
                IsPlayerAllowedToDraw = ready_data.IsPlayerAllowedToDraw;
                Round = ready_data.Round;
                MaximalRounds = ready_data.MaximalRounds;
                RoundEndTime = ready_data.RoundEndTime;
                if (ready_data.WordHints == null)
                {
                    wordHints = Array.Empty<IWordHint>();
                }
                else
                {
                    if (wordHints.Length != ready_data.WordHints.Count)
                    {
                        wordHints = new IWordHint[ready_data.WordHints.Count];
                    }
                    Parallel.For(0, wordHints.Length, (index) =>
                    {
                        WordHintData word_hint_data = ready_data.WordHints[index];
                        wordHints[index] = new WordHint(word_hint_data.Character, word_hint_data.Underline);
                    });
                }
                if (players.Length != ready_data.Players.Count)
                {
                    players = new IPlayer[ready_data.Players.Count];
                }
                Parallel.For(0, players.Length, (index) =>
                {
                    PlayerData player_data = ready_data.Players[index];
                    players[index] = new Player(player_data.ID, player_data.Name, player_data.Score, player_data.IsConnected, player_data.LastScore, player_data.Rank, player_data.State);
                });
                List<IDrawCommand> draw_commands = new List<IDrawCommand>();
                JObject json_object = JObject.Parse(json);
                if (json_object.ContainsKey("data"))
                {
                    if (json_object["data"] is JObject json_data_object)
                    {
                        if (json_data_object.ContainsKey("currentDrawing"))
                        {
                            if (json_data_object["currentDrawing"] is JArray json_draw_commands)
                            {
                                foreach (JToken json_token in json_draw_commands)
                                {
                                    if (json_token is JObject json_draw_command)
                                    {
                                        if (json_draw_command.ContainsKey("lineWidth"))
                                        {
                                            LineData line_data = json_draw_command.ToObject<LineData>();
                                            if (line_data != null)
                                            {
                                                draw_commands.Add(new DrawCommand(EDrawCommandType.Line, line_data.FromX, line_data.FromY, line_data.ToX, line_data.ToY, line_data.Color, line_data.LineWidth));
                                            }
                                        }
                                        else
                                        {
                                            FillData fill_data = json_draw_command.ToObject<FillData>();
                                            if (fill_data != null)
                                            {
                                                draw_commands.Add(new DrawCommand(EDrawCommandType.Fill, fill_data.X, fill_data.Y, default, default, fill_data.Color, 0.0f));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                OnReadyGameMessageReceived?.Invoke(ready_data.PlayerID, ready_data.IsPlayerAllowedToDraw, ready_data.OwnerID, ready_data.Round, ready_data.MaximalRounds, ready_data.RoundEndTime, wordHints, players, draw_commands, ready_data.GameState);
            }, MessageParseFailedEvent);
            AddMessageParser<NextTurnReceiveGameMessageData>((gameMessage, json) =>
            {
                NextTurnData next_turn_data = gameMessage.Data;
                IsPlayerAllowedToDraw = false;
                if (players.Length != next_turn_data.Players.Length)
                {
                    players = new IPlayer[next_turn_data.Players.Length];
                }
                Parallel.For(0, players.Length, (index) =>
                {
                    PlayerData player_data = next_turn_data.Players[index];
                    players[index] = new Player(player_data.ID, player_data.Name, player_data.Score, player_data.IsConnected, player_data.LastScore, player_data.Rank, player_data.State);
                });
                OnNextTurnGameMessageReceived?.Invoke(players, next_turn_data.Round, next_turn_data.RoundEndTime);
            }, MessageParseFailedEvent);
            AddMessageParser<UpdatePlayersReceiveGameMessageData>((gameMessage, json) =>
            {
                PlayerData[] player_array_data = gameMessage.Data;
                if (players.Length != player_array_data.Length)
                {
                    players = new IPlayer[player_array_data.Length];
                }
                Parallel.For(0, players.Length, (index) =>
                {
                    PlayerData player_data = player_array_data[index];
                    players[index] = new Player(player_data.ID, player_data.Name, player_data.Score, player_data.IsConnected, player_data.LastScore, player_data.Rank, player_data.State);
                });
                OnUpdatePlayersGameMessageReceived?.Invoke(players);
            }, MessageParseFailedEvent);
            AddMessageParser<UpdateWordhintReceiveGameMessageData>((gameMessage, json) =>
            {
                WordHintData[] word_hint_array_data = gameMessage.Data;
                if (wordHints.Length != word_hint_array_data.Length)
                {
                    wordHints = new IWordHint[word_hint_array_data.Length];
                }
                Parallel.For(0, players.Length, (index) =>
                {
                    WordHintData word_hint_data = word_hint_array_data[index];
                    wordHints[index] = new WordHint(word_hint_data.Character, word_hint_data.Underline);
                });
                OnUpdateWordhintGameMessageReceived?.Invoke(wordHints);
            }, MessageParseFailedEvent);
            AddMessageParser<MessageReceiveGameMessageData>((gameMessage, json) => OnMessageGameMessageReceived?.Invoke(gameMessage.Data.Author, gameMessage.Data.Content), MessageParseFailedEvent);
            AddMessageParser<NonGuessingPlayerMessageReceiveGameMessageData>((gameMessage, json) => OnNonGuessingPlayerMessageGameMessageReceived?.Invoke(gameMessage.Data.Author, gameMessage.Data.Content), MessageParseFailedEvent);
            AddMessageParser<SystemMessageReceiveGameMessageData>((gameMessage, json) => OnSystemMessageGameMessageReceived?.Invoke(gameMessage.Data), MessageParseFailedEvent);
            AddMessageParser<LineReceiveGameMessageData>((gameMessage, json) => OnLineGameMessageReceived?.Invoke(gameMessage.Data.FromX, gameMessage.Data.FromY, gameMessage.Data.ToX, gameMessage.Data.ToY, gameMessage.Data.Color, gameMessage.Data.LineWidth), MessageParseFailedEvent);
            AddMessageParser<FillReceiveGameMessageData>((gameMessage, json) => OnFillGameMessageReceived(gameMessage.Data.X, gameMessage.Data.Y, gameMessage.Data.Color), MessageParseFailedEvent);
            AddMessageParser<ClearDrawingBoardReceiveGameMessageData>((gameMessage, json) => OnClearDrawingBoardGameMessageReceived?.Invoke(), MessageParseFailedEvent);
            AddMessageParser<YourTurnReceiveGameMessageData>((gameMessage, json) =>
            {
                IsPlayerAllowedToDraw = true;
                OnYourTurnGameMessageReceived?.Invoke((string[])gameMessage.Data.Clone());
            }, MessageParseFailedEvent);
            AddMessageParser<CorrectGuessReceiveGameMessageData>((gameMessage, json) => OnCorrectGuessGameMessageReceived?.Invoke(gameMessage.Data), MessageParseFailedEvent);
            AddMessageParser<DrawingReceiveGameMessageData>((gameMessage, json) =>
            {
                List<IDrawCommand> draw_commands = new List<IDrawCommand>();
                JObject json_object = JObject.Parse(json);
                if (json_object.ContainsKey("data"))
                {
                    if (json_object["data"] is JArray json_draw_commands)
                    {
                        foreach (JToken json_token in json_draw_commands)
                        {
                            if (json_token is JObject json_draw_command)
                            {
                                if (json_draw_command.ContainsKey("lineWidth"))
                                {
                                    LineData line_data = json_draw_command.ToObject<LineData>();
                                    if (line_data != null)
                                    {
                                        draw_commands.Add(new DrawCommand(EDrawCommandType.Line, line_data.FromX, line_data.FromY, line_data.ToX, line_data.ToY, line_data.Color, line_data.LineWidth));
                                    }
                                }
                                else
                                {
                                    FillData fill_data = json_draw_command.ToObject<FillData>();
                                    if (fill_data != null)
                                    {
                                        draw_commands.Add(new DrawCommand(EDrawCommandType.Fill, fill_data.X, fill_data.Y, default, default, fill_data.Color, 0.0f));
                                    }
                                }
                            }
                        }
                    }
                }
                OnDrawingGameMessageReceived?.Invoke(draw_commands);
            }, MessageParseFailedEvent);
            webSocketReceiveThread = new Thread(async () =>
            {
                using (MemoryStream memory_stream = new MemoryStream())
                {
                    using (StreamReader reader = new StreamReader(memory_stream))
                    {
                        while (this.clientWebSocket.State == WebSocketState.Open)
                        {
                            try
                            {
                                WebSocketReceiveResult result = await this.clientWebSocket.ReceiveAsync(receiveBuffer, default);
                                if (result != null)
                                {
                                    memory_stream.Write(receiveBuffer.Array, 0, result.Count);
                                    if (result.EndOfMessage)
                                    {
                                        memory_stream.Seek(0L, SeekOrigin.Begin);
                                        receivedGameMessages.Enqueue(reader.ReadToEnd());
                                        memory_stream.Seek(0L, SeekOrigin.Begin);
                                        memory_stream.SetLength(0L);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine(e);
                            }
                        }
                    }
                }
            });
            webSocketReceiveThread.Start();
        }

        /// <summary>
        /// Listens to any message parse failed event
        /// </summary>
        /// <param name="expectedMessageType">Expected message type</param>
        /// <param name="message">Message</param>
        /// <param name="json">JSON</param>
        private void MessageParseFailedEvent<T>(string expectedMessageType, T message, string json) where T : IBaseGameMessageData
        {
            if (message == null)
            {
                Console.Error.WriteLine($"Received message is null of expected message type \"{ expectedMessageType }\".{ Environment.NewLine }{ Environment.NewLine }JSON:{ Environment.NewLine }{ json }");
            }
            else
            {
                Console.Error.WriteLine($"Message is invalid. Expected message type: \"{ expectedMessageType }\"; Current message type: { message.MessageType }{ Environment.NewLine }{ Environment.NewLine }JSON:{ Environment.NewLine }{ json }");
            }
        }

        /// <summary>
        /// Send WebSocket message (asynchronous)
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="message">Message</param>
        /// <returns>Task</returns>
        private Task SendWebSocketMessageAsync<T>(T message)
        {
            Task ret = Task.CompletedTask;
            if (clientWebSocket.State == WebSocketState.Open)
            {
                ret = clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message))), WebSocketMessageType.Text, true, default);
            }
            return ret;
        }

        /// <summary>
        /// Parses incoming message
        /// </summary>
        /// <param name="json">JSON</param>
        private void ParseMessage(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }
            BaseGameMessageData base_game_message_data = JsonConvert.DeserializeObject<BaseGameMessageData>(json);
            if (base_game_message_data != null)
            {
                if (gameMessageParsers.ContainsKey(base_game_message_data.MessageType))
                {
                    foreach (IBaseGameMessageParser game_message_parser in gameMessageParsers[base_game_message_data.MessageType])
                    {
                        game_message_parser.ParseMessage(json);
                    }
                }
                else
                {
                    OnUnknownGameMessageReceived?.Invoke(base_game_message_data, json);
                }
            }
        }

        /// <summary>
        /// Adds a game message parser
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="onGameMessageParsed">On message parsed</param>
        /// <param name="onGameMessageParseFailed">On message parse failed</param>
        /// <returns>Message parser</returns>
        public IGameMessageParser<T> AddMessageParser<T>(GameMessageParsedDelegate<T> onGameMessageParsed, GameMessageParseFailedDelegate<T> onGameMessageParseFailed = null) where T : IReceiveGameMessageData
        {
            IGameMessageParser<T> ret = new GameMessageParser<T>(onGameMessageParsed, onGameMessageParseFailed);
            List<IBaseGameMessageParser> game_message_parsers;
            if (gameMessageParsers.ContainsKey(ret.MessageType))
            {
                game_message_parsers = gameMessageParsers[ret.MessageType];
            }
            else
            {
                game_message_parsers = new List<IBaseGameMessageParser>();
                gameMessageParsers.Add(ret.MessageType, game_message_parsers);
            }
            game_message_parsers.Add(ret);
            return ret;
        }

        /// <summary>
        /// Removes the specified game message parser
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="gameMessageParser">Message parser</param>
        /// <returns>"true" if message parser was successfully removed, otherwise "false"</returns>
        public bool RemoveMessageParser<T>(IGameMessageParser<T> gameMessageParser) where T : IReceiveGameMessageData
        {
            if (gameMessageParser == null)
            {
                throw new ArgumentNullException(nameof(gameMessageParser));
            }
            bool ret = false;
            if (gameMessageParsers.ContainsKey(gameMessageParser.MessageType))
            {
                List<IBaseGameMessageParser> game_message_parsers = gameMessageParsers[gameMessageParser.MessageType];
                ret = game_message_parsers.Remove(gameMessageParser);
                if (ret && (game_message_parsers.Count <= 0))
                {
                    gameMessageParsers.Remove(gameMessageParser.MessageType);
                }
            }
            return ret;
        }

        /// <summary>
        /// Sends a "start" game message (asynchronous)
        /// </summary>
        /// <returns>Task</returns>
        public Task SendStartGameMessageAsync() => SendWebSocketMessageAsync(new StartSendGameMessageData());

        /// <summary>
        /// Sends a "name-change" game message (asynchronous)
        /// </summary>
        /// <param name="newUsername">New username</param>
        /// <returns>Task</returns>
        public Task SendNameChangeGameMessageAsync(string newUsername) => SendWebSocketMessageAsync(new NameChangeSendGameMessageData(newUsername));

        /// <summary>
        /// Sends a "request-drawing" game message (asynchronous)
        /// </summary>
        /// <returns>Task</returns>
        public Task SendRequestDrawingGameMessageAsync() => SendWebSocketMessageAsync(new RequestDrawingSendGameMessageData());

        /// <summary>
        /// Sends a "clear-drawing-board" game message (asynchronous)
        /// </summary>
        /// <returns>Task</returns>
        public Task SendClearDrawingBoardGameMessageAsync() => SendWebSocketMessageAsync(new ClearDrawingBoardSendGameMessageData());

        /// <summary>
        /// Sends a "fill" game message (asynchronous)
        /// </summary>
        /// <param name="positionX"></param>
        /// <param name="positionY"></param>
        /// <param name="color"></param>
        /// <returns>Task</returns>
        public Task SendFillGameMessageAsync(float positionX, float positionY, Color color) => SendWebSocketMessageAsync(new FillSendGameMessageData(positionX, positionY, color));

        /// <summary>
        /// Sends a "line" game message (asynchronous)
        /// </summary>
        /// <param name="fromX">Draw from X</param>
        /// <param name="fromY">Draw from Y</param>
        /// <param name="toX">Draw to X</param>
        /// <param name="toY">Draw to Y</param>
        /// <param name="color">Draw color</param>
        /// <param name="lineWidth">Line width</param>
        /// <returns>Task</returns>
        public Task SendLineGameMessageAsync(float fromX, float fromY, float toX, float toY, Color color, float lineWidth) => SendWebSocketMessageAsync(new LineSendGameMessageData(fromX, fromY, toX, toY, color, lineWidth));

        /// <summary>
        /// Sends a "choose-word" game message (asynchronous)
        /// </summary>
        /// <param name="index">Choose word index</param>
        /// <returns>Task</returns>
        public Task SendChooseWordGameMessageAsync(uint index) => SendWebSocketMessageAsync(new ChooseWordSendGameMessageData(index));

        /// <summary>
        /// Sends a "kick-vote" game message (asynchronous)
        /// </summary>
        /// <param name="toKickPlayer">To kick player</param>
        /// <returns></returns>
        public Task SendKickVoteAsync(IPlayer toKickPlayer)
        {
            if (toKickPlayer == null)
            {
                throw new ArgumentNullException(nameof(toKickPlayer));
            }
            return SendWebSocketMessageAsync(new KickVoteSendGameMessageData(toKickPlayer.ID));
        }

        /// <summary>
        /// Sends a "message" game message (asynchronous)
        /// </summary>
        /// <param name="content">Content</param>
        /// <returns>Task</returns>
        public Task SendMessageGameMessageAsync(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException();
            }
            return SendWebSocketMessageAsync(new MessageSendGameMessageData(content));
        }

        /// <summary>
        /// Processes events synchronously
        /// </summary>
        public void ProcessEvents()
        {
            while (receivedGameMessages.TryDequeue(out string json))
            {
                try
                {
                    ParseMessage(json);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }
        }

        /// <summary>
        /// Closes lobby
        /// </summary>
        public async void Close()
        {
            try
            {
                if (clientWebSocket.State == WebSocketState.Open)
                {
                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.Empty, null, default);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
            webSocketReceiveThread?.Join();
            webSocketReceiveThread = null;
            clientWebSocket.Dispose();
        }

        /// <summary>
        /// Disposes lobby
        /// </summary>
        public void Dispose() => Close();
    }
}
