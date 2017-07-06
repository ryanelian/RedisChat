using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;

namespace RedisChat
{
    public class ChatSocket
    {
        public static string ChatChannel => "Demo_Chat";

        private readonly RequestDelegate Next;

        public ChatSocket(RequestDelegate next)
        {
            this.Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path == "/" && context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                await Upgrade(socket);
            }

            await Next(context);
        }

        private async Task Upgrade(WebSocket socket)
        {
            var hub = Program.Redis.GetSubscriber();

            // Whenever someone sends a message to a Redis Channel, forward that message to the socket as a UTF-8 encoded string.
            Action<RedisChannel, RedisValue> cb = async (channel, value) =>
            {
                if (socket.State == WebSocketState.Open)
                {
                    var message = Encoding.UTF8.GetBytes(value.ToString());
                    await socket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            };

            // Subscribe socket callback to a Redis Channel.
            if (socket.State == WebSocketState.Open)
            {
                await hub.SubscribeAsync(ChatChannel, cb);
            }

            // Reserve 4 KB read buffer.
            var buffer = new byte[4 * 1024];

            while (socket.State == WebSocketState.Open)
            {
                // Non-blocking Event Loop
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                // Decode message as UTF-8 string, publish to Redis Channel
                if (result.CloseStatus.HasValue == false)
                {
                    await hub.PublishAsync(ChatChannel, Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
            }

            // Connection is no longer open. Unsubscribe callback and close socket.
            await hub.UnsubscribeAsync(ChatChannel, cb);
            await socket.CloseAsync(socket.CloseStatus.Value, socket.CloseStatusDescription, CancellationToken.None);
        }
    }
}
