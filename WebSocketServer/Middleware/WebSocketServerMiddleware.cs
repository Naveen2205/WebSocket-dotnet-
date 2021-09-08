using System;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using WebSocketServer.ConnectionManager;
using System.Linq;
using Newtonsoft.Json;

namespace WebSocketServer.Middleware{
    public class WebSocketServerMiddleware{
        private readonly RequestDelegate _next;
        private readonly WebSocketServerConnectionManager _manager;
        public WebSocketServerMiddleware(RequestDelegate next, WebSocketServerConnectionManager manager){
            _next = next;
            _manager = manager;
        }
        public async Task InvokeAsync(HttpContext context){
            if(context.WebSockets.IsWebSocketRequest){
                WebSocket websocket = await context.WebSockets.AcceptWebSocketAsync();
                string ConnID = _manager.AddSocket(websocket);
                Console.WriteLine("Websocket connected");
                await SendConnIDAsync(websocket, ConnID);
                await Receive(websocket, async (result, buffer)=>{
                    if(result.MessageType == WebSocketMessageType.Text){
                        Console.WriteLine("Receive->Text");
                        Console.WriteLine($"count = {result.Count}");
                        Console.WriteLine($"buffer = {buffer}");
                        Console.WriteLine($"Messager: {Encoding.UTF8.GetString(buffer, 0, result.Count)}");
                        await RouteJSONMessageAsync(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        return;
                    }
                    else if(result.MessageType == WebSocketMessageType.Close){
                        string id = _manager.GetAllSockets().FirstOrDefault(s => s.Value == websocket).Key;
                        Console.WriteLine("Receive->close");
                        _manager.GetAllSockets().TryRemove(id, out WebSocket socket);
                        Console.WriteLine("Managed connections -> "+ _manager.GetAllSockets().Count.ToString());
                        await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                        return;
                    }
                });
            }
            else{
                Console.WriteLine("Hello from 2nd Request Delegate");
                await _next(context);
            }
        }

        public async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage){
            var buffer = new byte[1024 * 4];
            while(socket.State == WebSocketState.Open){
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);
                handleMessage(result, buffer);
            }
        }

        public async Task SendConnIDAsync(WebSocket socket, string connID){
            var buffer = Encoding.UTF8.GetBytes("ConnID: "+ connID);
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task RouteJSONMessageAsync(string message){
            var routeOb = JsonConvert.DeserializeObject<dynamic>(message);
            if(Guid.TryParse(routeOb.To.ToString(), out Guid guidOutput)){
                Console.WriteLine("Targeted");
                var sock = _manager.GetAllSockets().FirstOrDefault(s => s.Key == routeOb.To.ToString());
                if(sock.Value != null){
                    if(sock.Value.State == WebSocketState.Open){
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(routeOb.Message.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else{
                        Console.WriteLine("Invalid recipient");
                    }
                }
            }
            else{
                Console.WriteLine("Broadcast");
                foreach(var sock in _manager.GetAllSockets()){
                    if(sock.Value.State == WebSocketState.Open){
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(routeOb.Message.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
        }
    }
}