using System.Windows;
using System.Windows.Input;

namespace ToolApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅
        private double mPrevWindowWidth;                        //  変更前のウィンドウ幅
        private string[] mProgramTitle = {                      //  プログラムタイトルリスト
            "マウスカーソル下の色取得",
            "ヤマレコリスト",
            "スクリーンキャプチャ",
            "サムネイル表示", 
            "フォト一覧",
            "フォルダ比較",
            "GPSデータ読込",
            "バイナリビューワ"
        };

        public MainWindow()
        {
            InitializeComponent();

            mWindowWidth = this.Width;
            mWindowHeight = this.Height;
            mPrevWindowWidth = mWindowWidth;

            WindowFormLoad();

            lbProgram.Items.Clear();
            foreach (string name in mProgramTitle)
                lbProgram.Items.Add(name);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //  Windowの状態を保存
            WindowFormSave();
        }

        private void lbProgram_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            executeProg();
        }

        private void lbProgram_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) {
                executeProg();
            }
        }

        /// <summary>
        /// 選択行のプログラムを実行
        /// </summary>
        private void executeProg()
        {
            Window programDlg = null;
            switch (lbProgram.SelectedIndex) {
                case 0: programDlg = new GetPixelColor(); break;
                case 1: programDlg = new YamaRecoList(); break;
                case 2: programDlg = new ScreenCapture(); break;
                case 3: programDlg = new PhotoView(); break;
                case 4: programDlg = new PhotoList(); break;
                case 5: programDlg = new DiffFolder(); break;
                case 6: programDlg = new FitConverter(); break;
                case 7: programDlg = new BinView(); break;
            }
            if (programDlg != null)
                programDlg.Show();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MainWindowWidth < 100 || Properties.Settings.Default.MainWindowHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.MainWindowHeight) {
                Properties.Settings.Default.MainWindowWidth = mWindowWidth;
                Properties.Settings.Default.MainWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.MainWindowTop;
                Left = Properties.Settings.Default.MainWindowLeft;
                Width = Properties.Settings.Default.MainWindowWidth;
                Height = Properties.Settings.Default.MainWindowHeight;
            }
            //  プログラムリストにフォーカスを設定
            lbProgram.Focus();
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MainWindowTop = Top;
            Properties.Settings.Default.MainWindowLeft = Left;
            Properties.Settings.Default.MainWindowWidth = Width;
            Properties.Settings.Default.MainWindowHeight = Height;
            Properties.Settings.Default.Save();
        }
    }
}
