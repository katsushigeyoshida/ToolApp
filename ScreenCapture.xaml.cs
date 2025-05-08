using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfLib;

namespace ToolApp
{
    /// <summary>
    /// ScreenCapture.xaml の相互作用ロジック
    /// </summary>
    public partial class ScreenCapture : Window
    {
        private int mMouseClickCount = -1;
        private System.Drawing.Point mPrevLoc;
        private WindowState mWinState;

        private YLib ylib = new YLib();
        private YDrawingShapes ydraw = new YDrawingShapes();

        public ScreenCapture()
        {
            InitializeComponent();

            //タイマーの設定、一定時間ごとにマウスカーソルの状態を見る
            var timer = new DispatcherTimer(DispatcherPriority.Normal);
            //timer.Interval = new TimeSpan(100000);//ナノ秒
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);//10ミリ秒毎（0.01秒毎）
            timer.Start();
            timer.Tick += Timer_Tick;
        }

        /// <summary>
        /// タイマーのTickイベント時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            //  マウスの座標の取得
            System.Drawing.Point p = System.Windows.Forms.Cursor.Position;//マウスカーソル位置取得

            if (ylib.IsClickDownLeft()) {
                //  マウスの左ボタンを押した時
                if (mMouseClickCount == 0) {
                    mPrevLoc = p;
                    mMouseClickCount++;
                }
            } else {
                //  マウスの左ボタンを話した時
                if (mMouseClickCount == 1) {
                    ImCaptureImage.Source = ydraw.bitmap2BitmapSource(ydraw.getScreen(mPrevLoc, p));
                    WindowState = mWinState;
                    Activate();
                    mMouseClickCount++;
                }
            }
        }

        /// <summary>
        /// 画面全体のコピー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtFullScreen_Click(object sender, RoutedEventArgs e)
        {
            mWinState = WindowState;
            WindowState = WindowState.Minimized;
            System.Threading.Thread.Sleep(500);

            //  フルスクリーンコピー
            ImCaptureImage.Source = ydraw.bitmap2BitmapSource(getFullScreenCapture());

            WindowState = mWinState;
            Activate();
        }

        /// <summary>
        /// アクティブウィンドウの画面キャプチャ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtActiveWindow_Click(object sender, RoutedEventArgs e)
        {
            //  アクティブウィンドウのキャプチャ
            mWinState = WindowState;
            WindowState = WindowState.Minimized;
            System.Threading.Thread.Sleep(500);

            ImCaptureImage.Source = ydraw.bitmap2BitmapSource(ydraw.getActiveWindowCapture());

            WindowState = mWinState;
            Activate();
        }

        /// <summary>
        /// 矩形領域を指定して画面をキャプチャ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtRectArea_Click(object sender, RoutedEventArgs e)
        {
            mWinState = WindowState;
            WindowState = WindowState.Minimized;
            mMouseClickCount = 0;
        }

        /// <summary>
        /// 全画面をキャプチャしたイメージから領域を切り取って表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtFullView_Click(object sender, RoutedEventArgs e)
        {
            fullViewCapture();
        }

        /// <summary>
        /// キャプチャデータをクリップボードにコピー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage((BitmapSource)ImCaptureImage.Source);
        }

        /// <summary>
        /// キャプチャしたデータをファイルに保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtSave_Click(object sender, RoutedEventArgs e)
        {
            imageSave();
        }

        private void ImageCopyMenu_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage((BitmapSource)ImCaptureImage.Source);
        }

        private void ImageFileMenu_Click(object sender, RoutedEventArgs e)
        {
            imageSave();
        }

        /// <summary>
        /// ファイルに保存
        /// </summary>
        private void imageSave()
        {
            List<string[]> filters = new List<string[]>() {
                    new string[] { "PNGファイル", "*.png;*.png" },
                    new string[] { "JPEGファイル", "*.jpg;*.jpg" },
                    new string[] { "すべてのファイル", "*.*"}
                };
            string filePath = ylib.saveFileSelect(null, "png");
            if (filePath.Length <= 0)
                return;
            if (0 == System.IO.Path.GetExtension(filePath).Length) {
                filePath = filePath + (filePath[filePath.Length - 1] == '.' ? "" : ".") + "png";
            }
            ydraw.SaveBitmapSourceToFile((BitmapSource)ImCaptureImage.Source, filePath);
        }

        /// <summary>
        /// 全画面をキャプチャする
        /// </summary>
        /// <returns>Bitmapデータ</returns>
        public Bitmap getFullScreenCapture()
        {
            Bitmap bitmap = new Bitmap(
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(new System.Drawing.Point(0, 0), new System.Drawing.Point(0, 0), bitmap.Size);
            graphics.Dispose();
            return bitmap;
        }

        /// <summary>
        /// 全画面をキャプチャしたイメージから領域を切り取って表示
        /// </summary>
        private void fullViewCapture()
        {
            //  自アプリ退避
            mWinState = WindowState;
            WindowState = WindowState.Minimized;
            System.Threading.Thread.Sleep(500);
            //  全画面をキャプチャ
            BitmapSource bitmapSource = ydraw.bitmap2BitmapSource(getFullScreenCapture()); ;
            //  自アプリを元に戻す
            WindowState = mWinState;
            Activate();
            //  キャプチャしたイメージを全画面表示し領域を切り取る
            FullView dlg = new FullView();
            dlg.mBitmapSource = bitmapSource;
            if (dlg.ShowDialog() == true) {
                Bitmap bitmap = ylib.cnvBitmapSource2Bitmap(bitmapSource);
                bitmap = ylib.trimingBitmap(bitmap, dlg.mStartPoint, dlg.mEndPoint);
                //  切り取った領域を貼り付ける
                ImCaptureImage.Source = ydraw.bitmap2BitmapSource(bitmap);
            }
        }
    }
}
