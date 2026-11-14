using Android.Media;
using Android.Views;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Text;

using Activity = Android.App.Activity;
using Encoding = Android.Media.Encoding;
using Stream = Android.Media.Stream;

namespace WifiSpeaker_Client {
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity {

        public static readonly int SOCKET_PORT = 25565; // ソケット通信のポート
        public static readonly int SAMPLE_RATE = 48000; // 1秒間に48000回サンプリング
        public static readonly int CHANNELS = 2;        // ステレオ（左右の2チャンネル）
        public static readonly int FRAME_SIZE = CHANNELS * sizeof(float);   // 1フレームのバイト数（2*4(float)= 8バイト）

        protected override void OnCreate(Bundle? savedInstanceState) {
            base.OnCreate(savedInstanceState);

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
            TextView? statusText = FindViewById<TextView>(Resource.Id.statusText);

            try {
                using TcpClient client = new TcpClient();
                await client.ConnectAsync(ipText.Text.Trim(), SOCKET_PORT);

                statusText.Text = "接続中";

                using NetworkStream stream = client.GetStream();

                int minBufferSize = AudioTrack.GetMinBufferSize(SAMPLE_RATE, ChannelOut.Stereo, Encoding.PcmFloat); // 再生するために必要な、最小バッファサイズを取得
                if (minBufferSize <= 0)
                    throw new Exception("AudioTrackの初期化に失敗しました");

                using AudioTrack audioTrack = new AudioTrack(
                    Stream.Music,   // 音楽再生用
                    SAMPLE_RATE,    // サンプル周波数（48000Hz）
                    ChannelOut.Stereo,  // ステレオ
                    Encoding.PcmFloat,  // PCM、Float、32Bit
                    Math.Max(minBufferSize, 65536), // バッファサイズ（最低でも65536バイト）
                    AudioTrackMode.Stream   // ストリーミング再生
                );

                byte[] buffer = new byte[8192 + FRAME_SIZE];    // 受け取るバッファ
                int remaining = 0;  // 受信できず余ったバイト数

                audioTrack.Play();

                await Task.Run(async () => {
                    while (true) {
                        int length = await stream.ReadAsync(buffer.AsMemory(remaining, 8192));  // データを受け取る
                        if (length == 0)
                            break;  // 受け取れなかった場合は、終了

                        int total = remaining + length; // 余ったバイト数 + 受け取ったバイト数 = 合計のバイト数
                        int validBytes = total - (total % FRAME_SIZE);  // 合計のバイト数 - (合計のバイト数 % 8バイト) = 次に受信まで保存するバイト数
                        // total = 8195
                        // validBytes = 8195 - (8195 % 8) = 8195 - 3 = 8192

                        if (validBytes > 0) {   //  次に受信まで保存するバイト数が1以上
                            float[] samples = new float[validBytes / 4];    // floatは4バイトなので、バイト数を4で割る。8192バイト / 4(float) = 2048
                            Buffer.BlockCopy(buffer, 0, samples, 0, validBytes);    // byte[]をfloat[]に変換
                            int offset = 0;
                            while (offset < samples.Length) {   // 書き込む開始位置が、float[]を確保した分を超えた場合、ループは終了する
                                int written = audioTrack.Write(
                                    samples,    // 音声データ
                                    offset,     // 書き込みを開始する位置
                                    samples.Length - offset,    // 書き込むfloat個数
                                    WriteMode.Blocking  // 書き込めるまで待機
                                );
                                if (written <= 0)
                                    throw new Exception($"AudioTrackの書き込みに失敗しました");

                                offset += written;  // 書き込めた分、開始位置をずらす
                            }
                        }

                        remaining = total - validBytes; // 余ったバイト数 = 合計のバイト数 - 次に受信まで保存するバイト数
                        if (remaining > 0) {
                            Buffer.BlockCopy(
                                buffer, // コピー元
                                validBytes, // コピー元の開始位置
                                buffer, // コピー先
                                0,  // コピー先の開始位置
                                remaining   // コピーするバイト数
                            );
                        }
                    }
                });

                audioTrack.Stop();
                statusText.Text = "切断";
            } catch (Exception ex) {
                statusText.Text = "エラー：" + ex.Message;
                Debug.WriteLine(ex);
            }
        }

    }
}