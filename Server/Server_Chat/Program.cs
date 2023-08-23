using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server_Chat
{
    public class Server
    {
        TcpListener serverChat = new TcpListener(IPAddress.Any, 8888);
        public List<Client> clients = new List<Client>();
        string str = "";
        public static Server Serv;
        static async Task Main(string[] args)
        {
            Serv = new Server();
            await Serv.RequestsClientsAsync();
        }
        async Task Start()
        {
            await RequestsClientsAsync();
        }
        protected internal async Task RequestsClientsAsync()
        {
            try
            {
                serverChat.Start();
                await Console.Out.WriteLineAsync("Started");
                while (true)
                {
                    TcpClient client = await serverChat.AcceptTcpClientAsync();
                    await Console.Out.WriteLineAsync("Connect");
                    Client clientObject = new Client(client,Serv);
                    clients.Add(clientObject);
                    await clientObject.Autorization();
                    await clientObject.networkStreamClient.FlushAsync();
                    _ = Task.Run(() => clientObject.RecieveAndSendAsync());

                }
            }
            catch (Exception ex)
            {

                await Console.Out.WriteLineAsync(ex.Message);
            }
          
        }

        protected internal async Task BroadcastMessageAsync(Json json)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                json.OnlineClient.Add($"{clients[i].Name}");
            }
            var send = JsonSerializer.Serialize(json);
            var sendArray = Encoding.UTF8.GetBytes(send);
            foreach (var client in clients)
            {
                    await client.networkStreamClient.WriteAsync(sendArray, 0, sendArray.Length);
                    await Console.Out.WriteLineAsync("Отправлено");
                /*if (client.ID != id)
                {
                }*/
            }
        }
       /* protected internal async Task VoiceMessage(Json json, string id)
        {
            json.message = $"[{dateTime.Day}/{dateTime.Month}/{dateTime.Year} {dateTime.Hour}:{dateTime.Minute}]\t" +
                    $"{client.Name}: >>> audio file <<<";
        }*/
        protected internal async void RemoveConnection(string id)
        {
            Client client = clients.FirstOrDefault(c => c.ID == id);
            Json jsonbuf=new Json();
            jsonbuf.status = "clientClose";
            jsonbuf.Name = client.Name;
            clients.Remove(client);
            client.Close();
            await BroadcastMessageAsync(jsonbuf);
        }
    }
    public class Json
    {
        public List<string> OnlineClient { get; set; }= new List<string>();
        public string Name { get; set; }
        public string message { get; set; } = "";
        public string status { get; set; }
        public string key { get; set; } = "";

    }
    public class Client
    {
        public string ID { get; set; } = Guid.NewGuid().ToString("N");
        public NetworkStream networkStreamClient { get; set; }
        public string Name { get; set; }
        TcpClient tcpClient;
        Server Server;
        public Client(TcpClient tcpClient,Server server)
        {
            this.tcpClient = tcpClient;
            Server = server;
            networkStreamClient=this.tcpClient.GetStream();
        }

        public async Task RecieveAndSendAsync()
        {
            try
            {
                while (tcpClient.Connected)
                {
                    byte[] getBytes = new byte[1024];
                    int count = await networkStreamClient.ReadAsync(getBytes, 0, getBytes.Length);
                    string result = Encoding.UTF8.GetString(getBytes, 0, count);
                    var clientResult = JsonSerializer.Deserialize<Json>(result);
                    Console.WriteLine("Принято!");
                    if(clientResult.status=="message")
                    await Server.BroadcastMessageAsync(clientResult);
                    else if(clientResult.status=="audio")
                    {
                        byte[] answer = Encoding.UTF8.GetBytes("ok");
                        await networkStreamClient.WriteAsync(answer, 0, answer.Length);

                        byte[] bytes = new byte[4096];
                        using (FileStream fs = new FileStream(Directory.GetCurrentDirectory(), FileMode.OpenOrCreate))
                        {
                            var count1 = await networkStreamClient.ReadAsync(bytes, 0, bytes.Length);
                            while (count > 0)
                            {
                                await fs.WriteAsync(bytes, 0, count);
                                if (count < 4096)
                                    break;
                                count = await networkStreamClient.ReadAsync(bytes, 0, bytes.Length);
                            }
                        }
                        await Server.BroadcastMessageAsync(clientResult);
                        /*answer = Encoding.UTF8.GetBytes($"ok");
                        await networkStreamClient.WriteAsync(answer, 0, answer.Length);

                        var buf = new byte[1024];
                        var bufCount = await networkStreamClient.ReadAsync(buf, 0, buf.Length);
                        var res = Encoding.UTF8.GetString(buf,0,bufCount);
                         await Server.VoiceMessage(clientResult, ID);*/
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                await Console.Out.WriteLineAsync($"Клиент {Name} отключтлся");
                await Console.Out.WriteLineAsync($"Колл-во клиентов: {Server.clients.Count}");
                Server.RemoveConnection(ID);
            }
        }                  
        public async Task Autorization()
        {
            byte[] getBytes = new byte[1024];
            int count = await networkStreamClient.ReadAsync(getBytes, 0, getBytes.Length);
            string result = Encoding.UTF8.GetString(getBytes, 0, count);
            var clientResult = JsonSerializer.Deserialize<Json>(result);
            Name=clientResult.Name;
            await OnlineClientAsync(clientResult);
        }
        public async Task OnlineClientAsync(Json json)
        {
            for (int i = 0; i < Server.clients.Count; i++)
            {
                json.OnlineClient.Add($"{Server.clients[i].Name}");
            }
            var send = JsonSerializer.Serialize(json);
            var sendArray = Encoding.UTF8.GetBytes(send);
            foreach (var client in Server.clients)
            {
                    await client.networkStreamClient.WriteAsync(sendArray, 0, sendArray.Length);
                    await Console.Out.WriteLineAsync("Отправлено");
            }
        }
        protected internal void Close()
        {
            networkStreamClient.Close();
            networkStreamClient.Dispose();
            tcpClient.Close();
            tcpClient.Dispose();
        }
    }
}
