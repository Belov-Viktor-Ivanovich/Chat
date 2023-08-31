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
using ControllerDLL_Chat;

namespace Interface_Chat
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// ScrollViewer.VerticalScrollBarVisibility="Auto"
    public partial class MainWindow : Window
    {
        
        public MainWindow()
        {
            InitializeComponent();
            
        }
        
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxLogin.Text != "")
            {
                //если с контроллера бз true
                if (true)
                {
                    Parse(await Controller_Chat.ConnectAsync(textBoxLogin.Text));
                    loginGrid.Visibility = Visibility.Collapsed;
                    gridViewList.Visibility = Visibility.Visible;
                    await Recieve();
                    //onlinelistBox.Items.Add($"{client.Name}");
                    /*var t = SendMassegeAutorizationAsync();
                    await t;
                    await networkStream.FlushAsync();
                    await ReciveMassege(networkStream);*/
                }
            }
            else
            {
                MessageBox.Show("Введите логин");
            }
        }
        private async Task Recieve()
        {
            while (true) 
            {
                Parse(await Controller_Chat.Recived());
                viewList.ScrollIntoView(viewList.Items[viewList.Items.Count - 1]);
            }
        }

        void Parse(string str)
        {
            onlinelistBox.Items.Clear();
            string[] strArr = str.Split(';');
            string[] strUsers = strArr[0].Split(',');
            for (int i = 0;i<strUsers.Length;i++)
            {
                onlinelistBox.Items.Add(strUsers[i].ToString());
            }
            if (strArr.Length > 1)
            {
                if (strArr[2] == "audio")
                {
                    addListViewEl("audio", strArr[1]);
                }
                else
                {
                    viewList.Items.Add(strArr[1]);
                }
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (textBoxSendMassege.Text != "")
            {
                await Controller_Chat.SendMassege("message", textBoxSendMassege.Text);
            }
        }

        //Отправка сообщения


        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }





        [Obsolete]
        private async void VoiceButon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                await Controller_Chat.ButtonVoiceDown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error VoiceButon_MouseLeftButtonDown");
            }
            
        }
        

        private async void VoiceButon_MouseLeftButtonUpAsync(object sender, MouseButtonEventArgs e)
        {
            await Controller_Chat.ButtonVoiceUp();
        }

        
        #region Для иконок аудио и других файлов
        void addListViewEl(string str, string name)
        {
            if (File.Exists($"{str}.ico"))
            {
                    StackPanel s = new StackPanel();
                    s.Orientation = Orientation.Horizontal;
                    Image img = new Image();
                    TextBlock tb = new TextBlock();
                    img.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + $@"\{str}.ico"));
                    img.Height = 16; img.Width = 16;
                    tb.FontSize = 16;
                    tb.Text = $" {name}";
                    s.Children.Add(img);
                    s.Children.Add(tb);
                    viewList.Items.Add(s);              
            }
        }
        #endregion

        private async void viewList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var a = ((((sender as ListView).SelectedItem) as StackPanel).Children[0]) as Image;
                var b = ((((sender as ListView).SelectedItem) as StackPanel).Children[1]) as TextBlock;

                await Controller_Chat.DownAndOpenFile(b.Text.Substring(1));


            }
            catch (Exception) { }

        }
    }
    

}
