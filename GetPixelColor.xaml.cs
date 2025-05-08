using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WpfLib;

namespace ToolApp
{
    /// <summary>
    /// GetPixelColor.xaml の相互作用ロジック
    /// 
    /// WPF？画面上のどこでもマウスカーソル下の色を取得
    /// https://gogowaten.hatenablog.com/entry/15890527
    /// 
    /// 参照にSystem.DrawingとSystem.Windows.Formsを追加する必要がある
    /// System.Drawingは画面全体をキャプチャするために
    /// System.Windows.Formsはマウスカーソルの画面上での座標の取得のため
    /// 一定時間ごとにマウスカーソル位置を取得、その座標の1ピクセルをキャプチャして、その色を取得
    /// </summary>
    public partial class GetPixelColor : Window
    {
        //クリックされているか判定用
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern short GetKeyState(int nVirtkey);

        List<string> mColorValueList = new List<string>();
        YLib ylib = new YLib();

        public GetPixelColor()
        {
            InitializeComponent();

            //タイトルバーにアプリの名前（アセンブリ名）表示
            var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Title = info.ProductName;

            //タイマーの設定、一定時間ごとにマウスカーソルの状態を見る
            var timer = new DispatcherTimer(DispatcherPriority.Normal);
            //timer.Interval = new TimeSpan(100000);//ナノ秒
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);//10ミリ秒毎（0.01秒毎）
            timer.Start();
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            System.Drawing.Point p = System.Windows.Forms.Cursor.Position;//マウスカーソル位置取得
            Color c = getPixelColor(p.X, p.Y);//マウスカーソル位置の色取得
            var b = new SolidColorBrush(c);
            MyTextBlockColor.Background = b;
            MyTextBlockColor.Text = c.ToString();
            MyTextBlockCursorLocation.Text = $"マウスの位置 = {System.Windows.Forms.Cursor.Position}";

            if (IsClickDownLeft()) {
                try {
                    //MyTextBlockGetColor.Background = b;
                    MyTextBlockGetColor.Text = "左クリックで取得した色: " + c.ToString();
                    Clipboard.SetText(c.ToString());
                    mColorValueList.Add(c.ToString());
                } catch (Exception ex) {
                    //MessageBox.Show(ex.ToString());
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }
        private bool IsClickDownLeft()
        {
            //マウス左ボタン(0x01)の状態、押されていたらマイナス値(-127)、なかったら0
            return GetKeyState(0x01) < 0;
        }

        private bool IsClickDownRight()
        {
            //マウス右ボタン(0x02)の状態、押されていたらマイナス値(-127)、なかったら0
            return GetKeyState(0x02) < 0;
        }

        //画面上の指定座標の1ピクセルの色を返す
        private Color getPixelColor(int x, int y)
        {
            //1x1サイズのBitmap作成
            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(
                1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                using (var bmpGraphics = System.Drawing.Graphics.FromImage(bitmap)) {
                    //画面全体をキャプチャして指定座標の1ピクセルだけBitmapにコピー
                    bmpGraphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(1, 1));
                    //ピクセルの色取得
                    System.Drawing.Color color = bitmap.GetPixel(0, 0);
                    //WPF用にSystem.Windows.Media.Colorに変換して返す
                    return Color.FromArgb(color.A, color.R, color.G, color.B);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ylib.saveListData("PixelColor.csv", mColorValueList);
        }
    }
}
