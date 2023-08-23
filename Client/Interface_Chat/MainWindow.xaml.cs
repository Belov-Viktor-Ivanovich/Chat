using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NAudio;
using NAudio.Wave;
using NAudio.FileFormats;
using NAudio.CoreAudioApi;
using System.IO;

namespace Interface_Chat
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// ScrollViewer.VerticalScrollBarVisibility="Auto"
    public partial class MainWindow : Window
    {
        public static int num = 0;
        WaveIn input;
        WaveFileWriter writer;
        string outputFilename = "";
        static TcpClient tcpClient = new TcpClient();
        static NetworkStream networkStream;
        static NetworkStream networkStreamRecieve;
        static string Host = System.Net.Dns.GetHostName();
        static string IP = Dns.GetHostByName(Host).AddressList[0].ToString();
        static int port = 8888;
        static Json client = new Json();
        DateTime dateTime;
        public MainWindow()
        {
            InitializeComponent();
            
        }
        
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxLogin.Text != "")
            {
                client.Name = textBoxLogin.Text;
                tcpClient.Connect(IP, port); //подключение клиента
                networkStream = tcpClient.GetStream();
                networkStreamRecieve = tcpClient.GetStream();
                loginGrid.Visibility = Visibility.Collapsed;
                gridViewList.Visibility = Visibility.Visible;
                onlinelistBox.Items.Add($"{client.Name}");
                var t= SendMassegeAutorizationAsync();
                await t;
                await networkStream.FlushAsync();
                await ReciveMassege(networkStreamRecieve);
            }
            else
            {
                MessageBox.Show("Введите логин");
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (textBoxSendMassege.Text != "")
            {
                dateTime = DateTime.Now;
                client.message = textBoxSendMassege.Text;
                client.status = "message";
                await SendMassege();
            }
        }

        //Отправка сообщения
        static async Task SendMassege()
        {
            var send = JsonSerializer.Serialize(client);
            var sendArray = Encoding.UTF8.GetBytes(send);
            await networkStream.WriteAsync(sendArray, 0, sendArray.Length);
        }
        static async Task ReadOk()
        {
            var buf = new byte[1024];
            await networkStream.ReadAsync(buf, 0, buf.Length);
        }
        async Task SendMassegeAutorizationAsync()
        {
            var send = JsonSerializer.Serialize(client);
            var sendArray = Encoding.UTF8.GetBytes(send);
            await networkStream.WriteAsync(sendArray, 0, sendArray.Length);
            byte[] getBytes = new byte[1024];
            int count = await networkStream.ReadAsync(getBytes, 0, getBytes.Length);
            string result = Encoding.UTF8.GetString(getBytes, 0, count);
            var clientResult = JsonSerializer.Deserialize<Json>(result);
            onlinelistBox.Items.Clear();
            for (int i = 0; i < clientResult.OnlineClient.Count; i++)
            {
                onlinelistBox.Items.Add(clientResult.OnlineClient[i].ToString());
            }
        }
        //Получение
        async Task ReciveMassege(NetworkStream ns)
        {
            while (true)
            {
                try
                {
                    byte[] getBytes = new byte[1024];
                    int count = await ns.ReadAsync(getBytes, 0, getBytes.Length);
                    string result = Encoding.UTF8.GetString(getBytes, 0, count);
                    var clientResult = JsonSerializer.Deserialize<Json>(result);
                    onlinelistBox.Items.Clear();
                    for (int i = 0; i < clientResult.OnlineClient.Count; i++)
                    {
                        onlinelistBox.Items.Add(clientResult.OnlineClient[i].ToString());
                    }
                    dateTime = DateTime.Now;
                    if (clientResult.status == "message")
                    {
                        var str = $"[{dateTime.Day}/{dateTime.Month}/{dateTime.Year} {dateTime.Hour}:{dateTime.Minute}]\t" +
                    $"{clientResult.Name}: *** {clientResult.message} ***";
                        if (clientResult.message != "")
                            viewList.Items.Add(str);
                    }
                    else if(clientResult.status == "clientClose")
                    {
                        var str = $"[{dateTime.Day}/{dateTime.Month}/{dateTime.Year} {dateTime.Hour}:{dateTime.Minute}]\t" +
                    $"{clientResult.Name}: *** ОТКЛЮЧИЛСЯ!!! ***";
                        viewList.Items.Add(str);

                    }
                    else if(clientResult.status =="audio")
                    {
                        MessageBox.Show("сделать StackPanel,добавить в него иконку аудио и от кого и дату сообщения");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        [Obsolete]
        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                //Записываем данные из буфера в файл
                writer.WriteData(e.Buffer, 0, e.BytesRecorded);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "Error waveIn_DataAvailable");
            }
                
        }
        private void waveIn_RecordingStopped(object sender, EventArgs e)
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
                MessageBox.Show(ex.Message, "Error waveIn_RecordingStopped");
            }
            
        }
        void StopRecording()
        {
            input.StopRecording();
        }
        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
             Close();                    
        }

        [Obsolete]
        private void VoiceButon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
                MessageBox.Show(ex.Message, "Error VoiceButon_MouseLeftButtonDown");
            }
            
        }
        

        private async void VoiceButon_MouseLeftButtonUpAsync(object sender, MouseButtonEventArgs e)
        {
            if (input != null)
            {
                StopRecording();
                client.status = "audio";
                dateTime = DateTime.Now;
                client.message = $"{outputFilename}";
                await SendMassege();
                await ReadOk();
                byte[] data = File.ReadAllBytes($"{Directory.GetCurrentDirectory()}\\{outputFilename}.wav");
                await networkStream.WriteAsync(data, 0, data.Length);
            }
        }

        
        #region Для иконок аудио и других файлов
        void addListViewEl(string str, string name, DateTime dateTime)
        {
            if (File.Exists($"{str}.ico"))
            {
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes($"{str}.ico")))
                {
                    StackPanel s = new StackPanel();
                    s.Orientation = Orientation.Horizontal;
                    Image img = new Image();
                    TextBlock tb = new TextBlock();
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    img.Source = bitmapImage;
                    img.Height = 16; img.Width = 16;

                    tb.FontSize = 16;
                    tb.Text = $" {name}";
                    s.Children.Add(img);
                    s.Children.Add(tb);
                    viewList.Items.Add(s);
                    viewList.Visibility = Visibility.Visible;
                    ms.Close();
                    ms.Dispose();
                }

            }
            else
            {
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes($"unknown.ico")))
                {
                    StackPanel s = new StackPanel();
                    s.Orientation = Orientation.Horizontal;
                    Image img = new Image();
                    TextBlock tb = new TextBlock();
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    img.Source = bitmapImage;
                    img.Height = 16; img.Width = 16;

                    tb.FontSize = 16;
                    tb.Text = $" {name}";
                    s.Children.Add(img);
                    s.Children.Add(tb);
                    viewList.Items.Add(s);
                    viewList.Visibility = Visibility.Visible;
                    ms.Close();
                    ms.Dispose();
                }
            }
        }
        #endregion
    }
    public class Json
    {

        public List<string> OnlineClient { get; set; }=new List<string>();
        public string Name { get; set; }
        public string message { get; set; } = "";
        public string status { get; set; }
        public string key { get; set; } = "";
    }

}
