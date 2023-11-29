using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;

class Program
{
    private static List<WebSocket> _clients = new List<WebSocket>();
    static async Task Main(string[] args)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();

        Console.WriteLine("Listening...");

        while (true)
        {
            var context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                Task.Run(() => ProcessWebSocketRequest(context));
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    static async Task ProcessWebSocketRequest(HttpListenerContext context)
    {
        var ws = await context.AcceptWebSocketAsync(subProtocol: null);
        Console.WriteLine("WebSocket connected");
        _clients.Add(ws.WebSocket);
        await Echo(ws.WebSocket);
    }

    static async Task Echo(WebSocket ws)
    {
        var buffer = new byte[1024 * 4];
        while (true)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("Received: " + message);

                var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                foreach(var client in _clients)
                {
                    await client.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
                }
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("WebSocket closed");
                break;
            }
        }
    }
}