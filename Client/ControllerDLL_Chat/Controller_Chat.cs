using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using NAudio.Wave;
using System.IO;
using System.Media;
using System.Runtime.InteropServices.ComTypes;

namespace ControllerDLL_Chat
{
    public class Controller_Chat
    {
        public static int num = 0;
        static WaveIn input;
        static WaveFileWriter writer;
        static string outputFilename = "";
        static TcpClient tcpClient = new TcpClient();
        static NetworkStream networkStream;
        static string Host = System.Net.Dns.GetHostName();
        static string IP = Dns.GetHostByName(Host).AddressList[0].ToString();
        static int port = 8888;
        static int port2 = 9000;
        static Json client = new Json();
        static DateTime dateTime;
        static SoundPlayer soundPlayer;
        static bool flag = true;
        static int ccc = 0;


        public static async Task<string> ConnectAsync(string Name)
        {
            await tcpClient.ConnectAsync(IP, port); //подключение клиента
            networkStream = tcpClient.GetStream();
            return await SendMassegeAutorizationAsync(Name);
        }
        //отправка
        public static async Task SendMassege(string status, string massege)
        {
            client.message = massege;
            client.status = status;
            var send = JsonSerializer.Serialize(client);
            var sendArray = Encoding.UTF8.GetBytes(send);
            await networkStream.WriteAsync(sendArray, 0, sendArray.Length);
        }
        public static async Task<string> Recived()
        {
            return await ReciveMassegeAsync(networkStream);
        }
        //Получение
        public static async Task<string> ReciveMassegeAsync(NetworkStream ns)
        {
            string message = "";
            try
            {                
                byte[] getBytes = new byte[512];
                int count = await ns.ReadAsync(getBytes, 0, getBytes.Length);

                string result = Encoding.UTF8.GetString(getBytes, 0, count);
                var clientResult = JsonSerializer.Deserialize<Json>(result);
                //onlinelistBox.Items.Clear();
                for (int i = 0; i < clientResult.OnlineClient.Count; i++)
                {
                    if (i != 0) message += ",";
                    message += clientResult.OnlineClient[i].ToString();
                }
                dateTime = DateTime.Now;
                if (clientResult.status == "message")
                {
                    message += $";[{dateTime.Day}/{dateTime.Month}/{dateTime.Year} {dateTime.Hour}:{dateTime.Minute}]\t" +
                $"{clientResult.Name}: *** {clientResult.message} ***;";
                    message += "message";
                    /*if (clientResult.message != "") { }*/
                }
                else if (clientResult.status == "clientClose")
                {
                    message += $";[{dateTime.Day}/{dateTime.Month}/{dateTime.Year} {dateTime.Hour}:{dateTime.Minute}]\t" +
                $"{clientResult.Name}: *** ОТКЛЮЧИЛСЯ!!! ***;";
                    message += "clientClose";
                }
                else if (clientResult.status == "audio")
                {
                    message += $";{clientResult.message};";
                    message += "audio";
                }
                else if (clientResult.status == "upload")
                {
                    dateTime = DateTime.Now;
                    message += $";{client.Name}_{dateTime.Year}.{dateTime.Month}.{dateTime.Day} {dateTime.Hour}-{dateTime.Minute}-{dateTime.Second}: {clientResult.message};";
                    message += "upload";
                }
                else
                {
                    message += $";[{dateTime.Day}/{dateTime.Month}/{dateTime.Year} {dateTime.Hour}:{dateTime.Minute}]\t" +
                $"{clientResult.Name}: *** ПОДКЛЮЧИЛСЯ!!! ***;";
                    message += "connect";
                }
                return message;
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                return message;
            }

        }
        public static async Task<string> SendMassegeAutorizationAsync(string Name)
        {
            string massege = "";
            client.Name = Name;
            dateTime = DateTime.Now;
            //не нужно,проверить
            /*client.message= $";[{dateTime.Day}/{dateTime.Month}/{dateTime.Year} {dateTime.Hour}:{dateTime.Minute}]\t" +
                $"{client.Name}: *** ПОДКЛЮЧИЛСЯ!!! ***";*/
            var send = JsonSerializer.Serialize(client);
            var sendArray = Encoding.UTF8.GetBytes(send);
            await networkStream.WriteAsync(sendArray, 0, sendArray.Length);
            byte[] getBytes = new byte[4096];
            int count = await networkStream.ReadAsync(getBytes, 0, getBytes.Length);
            string result = Encoding.UTF8.GetString(getBytes, 0, count);
            var clientResult = JsonSerializer.Deserialize<Json>(result);
            for (int i = 0; i < clientResult.OnlineClient.Count; i++)
            {
                if (i != 0) massege += ",";
                massege += clientResult.OnlineClient[i].ToString();
            }
            massege += $";[{dateTime.Day}/{dateTime.Month}/{dateTime.Year} {dateTime.Hour}:{dateTime.Minute}]\t" +
                $"{clientResult.Name}: *** ПОДКЛЮЧИЛСЯ!!! ***;";
            massege += "connect";
            return massege;
        }

        #region Audio
        public static async Task ButtonVoiceDown()
        {
            try
            {

                dateTime = DateTime.Now;
                outputFilename = $"{client.Name}_{dateTime.Year}.{dateTime.Month}.{dateTime.Day} {dateTime.Hour}-{dateTime.Minute}-{dateTime.Second}.wav";
                input = new WaveIn();
                input.DeviceNumber = 0;
                input.DataAvailable += waveIn_DataAvailable;
                input.RecordingStopped += new EventHandler<StoppedEventArgs>(waveIn_RecordingStopped);
                input.WaveFormat = new WaveFormat(8000, 1);
                writer = new WaveFileWriter(outputFilename, input.WaveFormat);
                input.StartRecording();
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
        }
        public static async Task ButtonVoiceUp()
        {
            if (input != null)
            {
                StopRecording();
                client.status = "audio";
                dateTime = DateTime.Now;
                client.message = $"{outputFilename}";
                var send = JsonSerializer.Serialize(client);
                var sendArray = Encoding.UTF8.GetBytes(send);
                await networkStream.WriteAsync(sendArray, 0, sendArray.Length);
                byte[] data = File.ReadAllBytes($@"{Directory.GetCurrentDirectory()}\{outputFilename}");
                await networkStream.WriteAsync(data, 0, data.Length);
                File.Delete($@"{Directory.GetCurrentDirectory()}\{outputFilename}");
            }
        }
        public static async Task UploadFile(string name,string path)
        {
            try
            {
                dateTime = DateTime.Now;
                client.status = "upload";
                client.message = name;
                var send = JsonSerializer.Serialize(client);
                var sendArray = Encoding.UTF8.GetBytes(send);
                await networkStream.WriteAsync(sendArray, 0, sendArray.Length);
                byte[] data = File.ReadAllBytes(path);
                await networkStream.WriteAsync(data, 0, data.Length);

            }
            catch (Exception)
            {

                throw;
            }

        }
        public static async Task DownAndOpenFile(string filename)
        {
            TcpClient tcpClientRead = new TcpClient();
            NetworkStream networkStreamRead;
            try
            {
                await tcpClientRead.ConnectAsync(IP, port2); //подключение клиента
                networkStreamRead = tcpClientRead.GetStream();
                client.message = filename;
                if (filename.Substring(filename.LastIndexOf(".") + 1) == "wav")
                    client.status = "audio";
                else client.status = "upload";
                var send = JsonSerializer.Serialize(client);
                var sendArray = Encoding.UTF8.GetBytes(send);
                await networkStreamRead.WriteAsync(sendArray, 0, sendArray.Length);
                byte[] bytes = new byte[4096];
                int ss = 0;
                using (FileStream fs = new FileStream($@"{Directory.GetCurrentDirectory()}\{filename}", FileMode.OpenOrCreate))
                {
                    var count = await networkStreamRead.ReadAsync(bytes, 0, bytes.Length);
                    ss += count;
                    while (count > 0)
                    {
                        await fs.WriteAsync(bytes, 0, count);
                        if (count < 4096)
                            break;
                        count = await networkStreamRead.ReadAsync(bytes, 0, bytes.Length);
                        ss += count;
                    }
                }
                soundPlayer = new SoundPlayer($@"{Directory.GetCurrentDirectory()}\{filename}");
                soundPlayer.Load();
                soundPlayer.PlaySync();
                tcpClientRead.Close();
                tcpClientRead.Dispose();
            }
            catch (Exception ex)
            {
                tcpClientRead.Close();
                tcpClientRead.Dispose();
            }

        }
        static async Task UploadFile(NetworkStream stream, string path, string nameFile)
        {

            byte[] bytes = new byte[4096];
            using (FileStream fs = new FileStream(path + $@"\{nameFile}", FileMode.OpenOrCreate))
            {
                var count1 = await stream.ReadAsync(bytes, 0, bytes.Length);
                while (count1 > 0)
                {
                    await fs.WriteAsync(bytes, 0, count1);
                    if (count1 < 4096)
                        break;
                    count1 = await stream.ReadAsync(bytes, 0, bytes.Length);
                }
            }
        }
        [Obsolete]
        static void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                //Записываем данные из буфера в файл
                writer.WriteData(e.Buffer, 0, e.BytesRecorded);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message, "Error waveIn_DataAvailable");
            }

        }
        static void waveIn_RecordingStopped(object sender, EventArgs e)
        {
            try
            {
                input.Dispose();
                input = null;
                writer.Close();
                writer = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "Error waveIn_RecordingStopped");
            }

        }
        static void StopRecording()
        {
            input.StopRecording();
        }
        
        #endregion
    }
    public class Json
    {

        public List<string> OnlineClient { get; set; } = new List<string>();
        public string Name { get; set; }
        public string message { get; set; } = "";
        public string status { get; set; }
        public string key { get; set; } = "";
    }
}
