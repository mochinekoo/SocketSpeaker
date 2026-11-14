using Android.Content;
using Android.Media;
using Android.Views;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Text;

using Activity = Android.App.Activity;

namespace WifiSpeaker_Client {
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity {

        public static string IP = null;

        protected override void OnCreate(Bundle? savedInstanceState) {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            TextView? statusText = FindViewById<TextView>(Resource.Id.statusText);
            statusText.Text = "接続状態：待機中";

            Button? button = FindViewById<Button>(Resource.Id.button);
            button.Click += async (sender, e) => {
                Debug.WriteLine("Click");
                EditText? ipText = FindViewById<EditText>(Resource.Id.ipText);
                MainActivity.IP = ipText.Text;

                var intent = new Intent(this, typeof(SpeakerService));
                StartForegroundService(intent);
            };
        }

    }
}