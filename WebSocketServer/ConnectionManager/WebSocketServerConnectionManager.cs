using System;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace WebSocketServer.ConnectionManager{
    public class WebSocketServerConnectionManager{
        private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public string AddSocket(WebSocket socket){
            string ConnID = Guid.NewGuid().ToString();
            _sockets.TryAdd(ConnID, socket);
            Console.WriteLine("WebSocketServerConnectionManager-> AddSocket: WebSocket added with ID: "+ ConnID);
            return ConnID;
        }

        public ConcurrentDictionary<string, WebSocket> GetAllSockets(){
            return _sockets;
        }
    }
}