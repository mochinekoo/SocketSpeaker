using Android.Views;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

using Activity = Android.App.Activity;

namespace WifiSpeaker_Client
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState) {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            TextView? statusText = FindViewById<TextView>(Resource.Id.statusText);
            statusText.Text = "接続状態：待機中";

            Button? button = FindViewById<Button>(Resource.Id.button);
            button.Click += async (sender, e) => {
                await InitSocket();
            };
        }

        public async Task InitSocket() {
            EditText? ipText = FindViewById<EditText>(Resource.Id.ipText);
            EditText? textBuffer = FindViewById<EditText>(Resource.Id.socketBuffer);

            using TcpClient client = new TcpClient();
            await client.ConnectAsync(ipText.Text, 25565);

            using NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            await Task.Run(async () => {
                while (true) {

                    int length = await stream.ReadAsync(buffer);
                    if (length > 0) {
                        string response = Encoding.UTF8.GetString(buffer, 0, length);

                        RunOnUiThread(() => {
                            textBuffer.Text = textBuffer.Text + "\n" + response;
                        });
                    }

                }
            });
        }
    }
}