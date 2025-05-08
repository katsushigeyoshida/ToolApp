using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using WpfLib;

namespace ToolApp
{
    /// <summary>
    /// BinView.xaml の相互作用ロジック
    /// </summary>
    public partial class BinView : Window
    {
        private YLib ylib = new YLib();
        private List<string> mFileList = new List<string>();
        private string mFileListName = "BinFieList.csv";
        private byte[] mBinData;
        private string[] mDataType = {                          //  データの種別
            "byte", "ascii", "int8", "int16", "int32", "int64", "float", "double", "日時", "時間", "時間2", "度1", "度2", "度3" };
        private int[] mDataStep = {                             //  データサイズ
             1,      1,       1,      2,       4,       8,       4,       8,        4,      4,      4,       4,     4,     4    };
        private string[] mDataForm = {                          //  表示フォーマット
            "X2",   "X1",    "D3",   "D5",    "D10",   "D20",   "D19",   "F6",     "F6",   "F6",   "F6",    "F6",  "F6",  "F6" };
        private int[] mCharCount = {                            //  表示文字数
             2,      1,       3,      5,       10,      20,      14,      22,       19,     12,     9,      22,    22,    12 };

        public BinView()
        {
            InitializeComponent();

            tbBinView.FontFamily = new System.Windows.Media.FontFamily("MS Gothic");
            tbBinView.FontSize = 10;
            tbStart.Text = "0";
            tbColCount.Text = "16";
            cbEndian.IsChecked = true;
            cbDataType.ItemsSource = mDataType;
            cbDataType.SelectedIndex = 0;
            cbFontSize.ItemsSource = new List<double>() {
                 8, 9, 10, 11, 11.5, 12, 13, 14, 15, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72
            };
            cbFontSize.SelectedIndex = 3;

            //  ファイルリスト読込
            if (File.Exists(mFileListName)) {
                List<string> fileList = ylib.loadListData(mFileListName);
                if (0 < fileList.Count) {
                    cbFileSelect.Items.Clear();
                    foreach (string file in fileList)
                        if (File.Exists(file))
                            cbFileSelect.Items.Add(file);
                }
            }
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (cbFileSelect.Items.Count > 0) {
                //  ファイルリスト保存
                List<string> fileList = new List<string>();
                for (int i = 0; i < cbFileSelect.Items.Count; i++)
                    fileList.Add(cbFileSelect.Items[i].ToString());
                ylib.saveListData(mFileListName, fileList);
            }
        }

        /// <summary>
        /// [ダブルクリック]でファイルの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFileSelect_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string folder = 0 < cbFileSelect.Text.Length ? Path.GetDirectoryName(cbFileSelect.Text) : ".";
            string path = ylib.fileSelect(folder, "bin,fit,*");
            if (0 < path.Length) {
                cbFileSelect.Items.Insert(0, path);
                cbFileSelect.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// データの再表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btLoad_Click(object sender, RoutedEventArgs e)
        {
            dumpData(mBinData);
        }

        /// <summary>
        /// データファイル切替
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFileSelect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string path = cbFileSelect.Items[cbFileSelect.SelectedIndex].ToString();
            loadData(path);
        }

        /// <summary>
        /// 表示データタイプ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbDataType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            dumpData(mBinData);
        }

        /// <summary>
        /// エンディアン変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbEndian_Click(object sender, RoutedEventArgs e)
        {
            dumpData(mBinData);
        }

        /// <summary>
        /// 表示文字サイズの設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFontSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            tbBinView.FontSize = ylib.doubleParse(cbFontSize.Items[cbFontSize.SelectedIndex].ToString());
            dumpData(mBinData);
        }

        /// <summary>
        /// バイナリデータの読込
        /// </summary>
        /// <param name="path"></param>
        private void loadData(string path)
        {
            if (0 < path.Length) {
                mBinData = ylib.loadBinData(path);
                tbFileProp.Text = "size = " + mBinData.Length.ToString();
                dumpData(mBinData);
            }
        }

        /// <summary>
        /// バイナリデータの表示
        /// </summary>
        /// <param name="data">バイナリデータ</param>
        private void dumpData(byte[] data)
        {
            if (data == null || data.Length == 0) return;

            int start = ylib.intParse(tbStart.Text.ToString());         //  開始位置(byte)
            int typeSize = mDataStep[cbDataType.SelectedIndex];         //  単位データサイズ
            int rowSize = ylib.intParse(tbColCount.Text.ToString());    //  １行のバイト数
            int charCount = mCharCount[cbDataType.SelectedIndex];
            string colForm = $"X{charCount}";                           //  列タイトルのフォーマット
            string sep = new string('-', charCount);                    //  セパレータ
            bool endian = cbEndian.IsChecked == true;                   //  リトルエンディアン

            StringBuilder buf = new StringBuilder();
            //  桁数タイトル
            buf.Append(new string(' ', 6));
            for (int i = 0; i < rowSize; i += typeSize) {
                if (i % 8 == 0)
                    buf.Append(" ");
                if (1 < charCount)
                    buf.Append($"{i.ToString(colForm)} ");
                else
                    buf.Append($"{i.ToString(colForm).Substring(i.ToString(colForm).Length - 1, 1)}");
            }
            //  セパレータ
            buf.Append("\n" + new string(' ', 6));
            for (int i = 0; i < rowSize; i += typeSize) {
                if (i % 8 == 0)
                    buf.Append(" ");
                if (1 < charCount)
                    buf.Append($"{sep} ");
                else
                    buf.Append($"{sep}");
            }
            //  データ
            for (int row = start; row < data.Length; row += rowSize) {
                buf.Append($"\n{row:X5}:");
                for (int i = row; i < row + rowSize && i <= data.Length - typeSize; i += typeSize) {
                    if ((i - row) % 8 == 0)
                        buf.Append(" ");
                    string strdata = convByteStr(data, i, typeSize, endian);
                    if (1 < charCount)
                        buf.Append($"{strdata} ");
                    else
                        buf.Append($"{strdata}");
                }
            }
            tbBinView.Text = buf.ToString();
        }

        /// <summary>
        /// 指定位置のデータを文字列に変換
        /// </summary>
        /// <param name="data">byteデータ</param>
        /// <param name="pos">位置</param>
        /// <param name="size">データサイズ</param>
        /// <param name="littleEndien">エンディアン</param>
        /// <returns>変換文字列</returns>
        private string convByteStr(byte[] data, int pos, int size, bool littleEndien = true)
        {
            string buf = "";
            byte[] byteData = convByte(data, pos, size, littleEndien);
            string form = mDataForm[cbDataType.SelectedIndex];
            int charCount = mCharCount[cbDataType.SelectedIndex];
            switch (mDataType[cbDataType.SelectedIndex]) {
                case "byte": buf = byteData[0].ToString(form); break;
                case "ascii":
                    buf = buf = 0x20 <= byteData[0] && byteData[0] < 0x7f ?
                        ((char)byteData[0]).ToString() : ".";
                    break;
                case "int8": buf = byteData[0].ToString(form); break;
                case "int16": buf = BitConverter.ToUInt16(byteData, 0).ToString(form); break;
                case "int32": buf = BitConverter.ToUInt32(byteData, 0).ToString(form); break;
                case "int64": buf = BitConverter.ToUInt64(byteData, 0).ToString(form); break;
                case "float":
                    buf = BitConverter.ToSingle(byteData, 0).ToString().PadLeft(charCount);
                    break;
                case "double":
                    buf = BitConverter.ToDouble(byteData, 0).ToString().PadLeft(charCount);
                    break;
                case "日時":
                    var baseDate = new DateTime(1989, 12, 31, 0, 0, 0);
                    DateTime dt = baseDate.AddSeconds(BitConverter.ToUInt32(byteData, 0));
                    buf = dt.ToString("yyyy/MM/dd HH:mm:ss");
                    break;
                case "時間":                  //  時間 hhmmss.ss
                    buf = ((double)BitConverter.ToUInt32(byteData, 0) / 100).ToString().PadLeft(charCount);
                    break;
                case "時間2":                  //  秒 hhmmss.ss
                    int seconds = (int)BitConverter.ToUInt32(byteData, 0);
                    TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                    buf = timeSpan.ToString(@"hh\:mm\:ss");
                    break;
                case "度1":                    //  座標データ(秒→度)
                    buf = ((double)BitConverter.ToUInt32(byteData, 0) / 3600).ToString().PadLeft(charCount);
                    break;
                case "度2":                    //  座標データ(緯度・経度(semicircle→度))
                    buf = ((double)BitConverter.ToUInt32(byteData, 0) / Math.Pow(2, 31) * 180).ToString().PadLeft(charCount);
                    break;
                case "度3":                    //  座標データ(分→分)
                    buf = ((double)BitConverter.ToUInt32(byteData, 0) / 100000).ToString().PadLeft(charCount);
                    break;
            }
            return buf;
        }

        /// <summary>
        /// byteからサイズ分のデータを抽出(リトルエンディアンの時は逆順にする)
        /// </summary>
        /// <param name="data">byteデータ</param>
        /// <param name="pos">位置</param>
        /// <param name="size">データサイズ</param>
        /// <param name="littleEndien">エンディアン</param>
        /// <returns></returns>
        private byte[] convByte(byte[] data, int pos, int size, bool littleEndien = true)
        {
            byte[] ret = new byte[size];
            Array.Copy(data, pos, ret, 0, size);
            if (littleEndien)
                Array.Reverse(ret);
            return ret;
        }
    }
}

