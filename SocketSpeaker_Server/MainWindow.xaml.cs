using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Wave;

namespace WifiSpeaker_Server {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();


            foreach (var network in NetworkInterface.GetAllNetworkInterfaces()) {
                if (network.OperationalStatus == OperationalStatus.Up 
                    && network.NetworkInterfaceType != NetworkInterfaceType.Loopback) {
                    var properties = network.GetIPProperties();

                    if (properties.GatewayAddresses.Count > 0) {
                        foreach (var address in properties.UnicastAddresses) {
                            if (address.Address.AddressFamily == AddressFamily.InterNetwork) {
                                ClientIPText.Text = address.Address.ToString();
                                break;
                            }
                        }
                    }
                } 
            }

            
            StatusText.Text = "接続待機中...";
        }

        public async void OnLoad(object sender, RoutedEventArgs args) {
            await InitSocket();
        }

        public async Task InitSocket() {
            var server = new TcpListener(IPAddress.Any, 25565);
            server.Start();

            Debug.Print("接続待機中...");

            using TcpClient client = await server.AcceptTcpClientAsync();

            Debug.Print("接続完了");
            StatusText.Text = "接続中";

            using NetworkStream stream = client.GetStream();
            using WasapiLoopbackCapture capture = new();
            Debug.WriteLine(capture.WaveFormat.ToString());

            // byte[] data = Encoding.UTF8.GetBytes("ようこそ");
            // await stream.WriteAsync(data);

            capture.DataAvailable += (s, e) => {
                try {
                    stream.Write(e.Buffer, 0, e.BytesRecorded);
                } catch (Exception ex) {
                    Debug.WriteLine(ex);
                }
            };

            capture.StartRecording();


            byte[] buffer = new byte[1024];
            while (true) {
                int length = await stream.ReadAsync(buffer);

                if (length == 0)
                    break;
            }

            capture.StopRecording();
            server.Stop();
        }

    }
}