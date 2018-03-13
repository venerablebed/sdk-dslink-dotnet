using DSLink.Logger;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DSLink.Connection
{
    public class WebSocketConnector : Connector
    {
        private static readonly BaseLogger Log = LogManager.GetLogger();
        
        private ClientWebSocket _ws;
        private CancellationTokenSource _tokenSource;

        public WebSocketConnector(Configuration config)
            : base(config)
        {
        }

        protected override async Task<ConnectionState> Open()
        {
            _ws = new ClientWebSocket();
            _tokenSource = new CancellationTokenSource();
            await _ws.ConnectAsync(new Uri(WsUrl), CancellationToken.None);
            _startWatchTask();

            return _ws.State == WebSocketState.Open ? ConnectionState.Connected : ConnectionState.Disconnected;
        }

        protected override async Task<ConnectionState> Close()
        {
            _stopWatchTask();
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
            _ws = null;

            return ConnectionState.Disconnected;
        }

        protected override Task Write(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _tokenSource.Token);
        }

        protected override Task Write(byte[] data)
        {
            return _ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, _tokenSource.Token);
        }

        private void _startWatchTask()
        {
            Log.Debug("Starting watch task");
            Task.Run(async () =>
            {
                var token = _tokenSource.Token;

                while (_ws.State == WebSocketState.Open)
                {
                    var buffer = new byte[1024];
                    var bytes = new List<byte>();
                    var str = "";

                    RECV:
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                    if (result == null)
                    {
                        goto RECV;
                    }

                    var bufferUsed = result.Count;

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            Log.Debug($"Received close from broker: {result.CloseStatusDescription}");
                            EmitFailure();
                            break;
                        case WebSocketMessageType.Text:
                            str += Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                            if (!result.EndOfMessage)
                                goto RECV;
                            Receive(str);
                            break;
                        case WebSocketMessageType.Binary:
                            var newBytes = new byte[bufferUsed];
                            Array.Copy(buffer, newBytes, bufferUsed);
                            bytes.AddRange(newBytes);
                            if (!result.EndOfMessage)
                                goto RECV;
                            Receive(bytes.ToArray());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }

                return Task.CompletedTask;
            }, _tokenSource.Token);
        }

        private void _stopWatchTask()
        {
            Log.Debug("Stopping watch task");
            _tokenSource.Cancel();
        }
    }
}
