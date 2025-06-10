using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using WpfLib;

namespace ToolApp
{
    /// <summary>
    /// StatisticDlg.xaml の相互作用ロジック
    /// </summary>
    public partial class StatisticDlg : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅

        public byte[] mData;
        public int mStartPos = 0;
        public int mEndPos = 0;

        private YLib ylib = new YLib();
        private YCalc ycalc = new YCalc();

        public StatisticDlg()
        {
            InitializeComponent();

            WindowFormLoad();
            rb2Byte.IsChecked = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbStart.Text = mStartPos.ToString();
            tbEnd.Text = mEndPos.ToString();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.StatisticWidth < 100 ||
                Properties.Settings.Default.StatisticHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.BinViewHeight) {
                Properties.Settings.Default.StatisticWidth = mWindowWidth;
                Properties.Settings.Default.StatisticHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.StatisticTop;
                Left = Properties.Settings.Default.StatisticLeft;
                Width = Properties.Settings.Default.StatisticWidth;
                Height = Properties.Settings.Default.StatisticHeight;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.BinViewTop = Top;
            Properties.Settings.Default.BinViewLeft = Left;
            Properties.Settings.Default.BinViewWidth = Width;
            Properties.Settings.Default.BinViewHeight = Height;
            Properties.Settings.Default.Save();
        }

        private void btExec_Click(object sender, RoutedEventArgs e)
        {
            int size = 1;
            if (rb1Byte.IsChecked == true)
                size = 1;
            else if (rb2Byte.IsChecked == true)
                size = 2;
            else if (rb3Byte.IsChecked == true)
                size = 3;
            else if (rb4Byte.IsChecked == true)
                size = 4;
            mStartPos = (int)ycalc.expression(tbStart.Text.ToString());
            mEndPos = (int)ycalc.expression(tbEnd.Text.ToString());
            if (mStartPos >= mEndPos || mData.Length < mEndPos)
                mEndPos = mData.Length;
            statistic(mData, mStartPos, mEndPos, size);
        }

        /// <summary>
        /// 統計計算　データの出現率
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPos"></param>
        /// <param name="endPos"></param>
        private void statistic(byte[] data, int startPos, int endPos, int size)
        {
            Dictionary<string, int> statisticIabe = new Dictionary<string, int>();
            for (int i = startPos; i <= endPos - size; i++) {
                string byteStr = YLib.binary2HexString(data, i, size);
                if (statisticIabe.ContainsKey(byteStr))
                    statisticIabe[byteStr]++;
                else
                    statisticIabe.Add(byteStr, 1);
            }
            var sortedByValue = statisticIabe.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            StringBuilder buf = new StringBuilder();
            foreach (var kvp in sortedByValue) {
                buf.Append($"{kvp.Key}: {kvp.Value}\n");
            }
            tbStatisticView.Text = buf.ToString();
        }

    }
}
