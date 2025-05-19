using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfLib;

namespace ToolApp
{
    /// <summary>
    /// BinView.xaml の相互作用ロジック
    /// </summary>
    public partial class BinView : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅

        private string mFileListName = "BinFieList.csv";        //  ファイル名リスト保存ファイ露命
        private byte[] mBinData;                                //  読込データ
        private string[] mDataType = {                          //  データの種別
            "byte", "ascii", "int8", "int16", "int32", "int64", "float", "double", "カスタム", "日時", "時間", "時間2", "度1", "度2", "度3", "Epson57", "Epson53" };
        private int[] mDataTypeSizes = {                        //  データサイズ
             1,      1,       1,      2,       4,       8,       4,       8,        4,         4,      4,      4,       4,     4,     4,     0,       0,    };
        private string[] mDataForms = {                         //  表示フォーマット
            "X2",   "X1",    "D3",   "D5",    "D10",   "D20",   "D19",   "F6",     "D",         "F6",   "F6",   "F6",    "F6",  "F6",  "F6",   "",      "" };
        private int[] mDataDispSizes = {                        //  表示文字数
             2,      1,       3,      5,       10,      20,      14,      22,      4,            19,     12,     9,      22,    22,    12,     0,        0 };
        private List<double> mFontSizes = new List<double>() {
                 8, 9, 10, 11, 11.5, 12, 13, 14, 15, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72
            };
        private int mDataTypeSize = 0;
        private int mDataDispSize = 0;
        private string mDataForm = "";

        public InputBox mMemoDlg = null;                        //  メモダイヤログ

        private YLib ylib = new YLib();
        private YCalc ycalc = new YCalc();

        /// <summary>
        /// バイナリビューワ コンストラクタ
        /// </summary>
        public BinView()
        {
            InitializeComponent();

            WindowFormLoad();

            tbBinView.FontFamily = new FontFamily("MS Gothic");
            tbStart.Text = "0";
            tbColCount.Text = "16";
            cbEndian.IsChecked = true;
            cbDataType.ItemsSource = mDataType;
            cbDataType.SelectedIndex = 0;
            cbFontSize.ItemsSource = mFontSizes;
            cbFontSize.SelectedIndex = mFontSizes.FindIndex(p => p == tbBinView.FontSize);
            tbDataTypeSize.IsEnabled = false;
            tbDataTypeSize.Text = 3.ToString();

            //  ファイルリスト読込
            loadFileList(mFileListName);
        }

        /// <summary>
        /// 開始処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            setMemoText();
            if (mMemoDlg != null)
                mMemoDlg.Close();
            saveFileList(mFileListName);
            WindowFormSave();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.BinViewWidth < 100 ||
                Properties.Settings.Default.BinViewHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.BinViewHeight) {
                Properties.Settings.Default.BinViewWidth = mWindowWidth;
                Properties.Settings.Default.BinViewHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.BinViewTop;
                Left = Properties.Settings.Default.BinViewLeft;
                Width = Properties.Settings.Default.BinViewWidth;
                Height = Properties.Settings.Default.BinViewHeight;
            }
            tbBinView.FontSize = Properties.Settings.Default.BinViewFontSize < 6 ? 11 : Properties.Settings.Default.BinViewFontSize;
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            Properties.Settings.Default.BinViewFontSize = tbBinView.FontSize;
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.BinViewTop = Top;
            Properties.Settings.Default.BinViewLeft = Left;
            Properties.Settings.Default.BinViewWidth = Width;
            Properties.Settings.Default.BinViewHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [終了]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// [ダブルクリック]でファイルの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFileSelect_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string folder = 0 < cbFileSelect.Text.Length ? Path.GetDirectoryName(cbFileSelect.Text) : ".";
            string path = ylib.fileSelect(folder, "bin,fit");
            if (0 < path.Length) {
                cbFileSelect.Items.Insert(0, path);
                cbFileSelect.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// [再表示]データの再表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btLoad_Click(object sender, RoutedEventArgs e)
        {
            setDataTypePara(cbDataType.SelectedIndex, false);
            tbBinView.Text = dumpData(mBinData);
        }

        /// <summary>
        /// [先頭]ボタン(先頭から検索)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btTopSearch_Click(object sender, RoutedEventArgs e)
        {
            tbBinView.SelectionStart = 0;
            search();
        }

        /// <summary>
        /// [次]ボタン (カーソル位置から検索)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btNextSearch_Click(object sender, RoutedEventArgs e)
        {
            search();
        }

        /// <summary>
        /// [メモ]ボタン(メモダイヤログの表示)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btMemo_Click(object sender, RoutedEventArgs e)
        {
            memo();
        }

        /// <summary>
        /// 検索ワードコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbSearchMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("tbDec2ByteMenu") == 0) {
                //  Dec →Byte列変換
                tbSeachText.Text = dec2Byte(tbSeachText.Text, cbEndian.IsChecked == true);
            } else if (menuItem.Name.CompareTo("tbByte2DecMenu") == 0) {
                //  byte列 → Dec
                tbSeachText.Text = byte2Dec(tbSeachText.Text, cbEndian.IsChecked == true);
            } else if (menuItem.Name.CompareTo("tbReverseByteMenu") == 0) {
                //  Byte反転
                tbSeachText.Text = reverseByte(tbSeachText.Text);
            } else if (menuItem.Name.CompareTo("tbDeg2dmsMenu") == 0) {
                //  度ddd.ddd→ddd mm ss(bin)
                tbSeachText.Text = deg2dms(tbSeachText.Text);
            } else if (menuItem.Name.CompareTo("tbDeg2dmssMenu") == 0) {
                //  度ddd.ddd→ddd mm ssss
                tbSeachText.Text = deg2dmss(tbSeachText.Text);
            }
        }

        /// <summary>
        /// データファイル切替
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFileSelect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int index = cbFileSelect.SelectedIndex;
            if (index < 0) return;
            string path = cbFileSelect.Items[index].ToString();
            if (0 < index) {
                //  選択したファイルをリストの上位にして再選択
                if (cbFileSelect.Items.Contains(path))
                    cbFileSelect.Items.Remove(path);
                cbFileSelect.Items.Insert(0, path);
                cbFileSelect.SelectedIndex = 0;
            } else {
                //  選択したファイル(リストの最上位)をロード
                if (mMemoDlg != null)
                    mMemoDlg.Close();
                loadData(path);
            }
        }

        /// <summary>
        /// 表示データタイプ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbDataType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            setDataTypePara(cbDataType.SelectedIndex);
            tbBinView.Text = dumpData(mBinData);
        }

        /// <summary>
        /// エンディアン変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbEndian_Click(object sender, RoutedEventArgs e)
        {
            tbBinView.Text = dumpData(mBinData);
        }

        /// <summary>
        /// 表示文字サイズの設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFontSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            tbBinView.FontSize = ylib.doubleParse(cbFontSize.Items[cbFontSize.SelectedIndex].ToString());
            tbBinView.Text = dumpData(mBinData);
        }

        /// <summary>
        /// データタイプごとにパラメータの設定
        /// </summary>
        /// <param name="typeindex"></param>
        private void setDataTypePara(int typeindex, bool first = true)
        {
            mDataTypeSize = mDataTypeSizes[typeindex];          //  単位データサイズ
            mDataDispSize = mDataDispSizes[typeindex];          //  表示桁数
            mDataForm = mDataForms[typeindex];                  //  表示フォーマット

            tbDataTypeSize.IsEnabled = false;                   //  カスタムのデータサイズ
            if (mDataType[typeindex] == "カスタム") {
                tbDataTypeSize.IsEnabled = true;
                mDataTypeSize = ylib.intParse(tbDataTypeSize.Text);
                if (8 < mDataTypeSize) {
                    tbDataTypeSize.Text = "8";
                    mDataTypeSize = 7;
                }
                mDataDispSize = mDataTypeSize < 5 ? mDataTypeSize * 2 + 2 :
                    mDataTypeSize < 8 ? mDataTypeSize * 2 + 3 : mDataTypeSize * 2 + 4;
                mDataForm = $"D{mDataDispSize}";
            } else if (first && mDataType[typeindex] == "Epson57") {
                tbColCount.Text = "57";
                tbStart.Text = "21";
            } else if (first && mDataType[typeindex] == "Epson53") {
                tbColCount.Text = "53";
                tbStart.Text = "21";
            }
        }

        /// <summary>
        /// バイナリデータの表示
        /// EPSON57 length 57 delmitter 00 71  offset 21
        /// EPSON53 length 53 delmitter 00 10  offset 21
        /// </summary>
        /// <param name="data">バイナリデータ</param>
        private string dumpData(byte[] data)
        {
            if (data == null || data.Length == 0) return "";

            int start = (int)ycalc.expression(tbStart.Text.ToString());         //  開始位置(byte)
            int lineLength = (int)ycalc.expression(tbColCount.Text.ToString()); //  １行のバイト数
            bool endian = cbEndian.IsChecked == true;                   //  リトルエンディアン
            int typeSize = mDataTypeSize;                               //  単位データサイズ

            if (mDataType[cbDataType.SelectedIndex] == "Epson57") {
                tbStart.IsEnabled = true;
                tbColCount.IsEnabled = false;
                return dumpEpson(data, 57, new byte[] { 0x00, 0x71 }, start, endian);
            } else if (mDataType[cbDataType.SelectedIndex] == "Epson53") {
                tbStart.IsEnabled = true;
                tbColCount.IsEnabled = false;
                return dumpEpson(data, 53, new byte[] { 0x00, 0x10 }, start, endian);
            } else {
                tbStart.IsEnabled = true;
                tbColCount.IsEnabled = true;
                return dumpData(data, typeSize, start, lineLength, mDataDispSize, endian);
            }
        }

        /// <summary>
        /// バイナリデータの表示
        /// </summary>
        /// <param name="data">バイナリデータ</param>
        /// <param name="typeSize">単位データのサイズ</param>
        /// <param name="start">表示開始位置</param>
        /// <param name="lineLength">1行の表示バイト数</param>
        /// <param name="charCount">単位データの表示桁数</param>
        /// <param name="endian">エンディアン(true=little)</param>
        /// <returns>費用字文字列</returns>
        private string dumpData(byte[] data, int typeSize, int start, int lineLength, int charCount, bool endian)
        {
            string colForm = $"X{charCount}";                           //  列タイトルのフォーマット
            string sep = new string('-', charCount);                    //  セパレータ

            StringBuilder buf = new StringBuilder();
            //  桁数タイトル
            buf.Append(new string(' ', 7));
            for (int i = 0; i < lineLength; i += typeSize) {
                if (i % 8 == 0)
                    buf.Append(" ");
                if (1 < charCount)
                    buf.Append($"{i.ToString(colForm)} ");
                else
                    buf.Append($"{i.ToString(colForm).Substring(i.ToString(colForm).Length - 1, 1)}");
            }
            //  セパレータ
            buf.Append("\n" + new string(' ', 7));
            for (int i = 0; i < lineLength; i += typeSize) {
                if (i % 8 == 0)
                    buf.Append(" ");
                if (1 < charCount)
                    buf.Append($"{sep} ");
                else
                    buf.Append($"{sep}");
            }
            //  データ
            for (int row = start; row < data.Length; row += lineLength) {
                buf.Append($"\n{row:X6}:");
                for (int i = row; i < row + lineLength && i <= data.Length - typeSize; i += typeSize) {
                    if ((i - row) % 8 == 0)
                        buf.Append(" ");
                    string strdata = convByteStr(data, i, typeSize, endian);
                    if (1 < charCount)
                        buf.Append($"{strdata} ");
                    else
                        buf.Append($"{strdata}");
                }
            }
            return buf.ToString();
        }

        /// <summary>
        /// Epson GPS Watch Workout Binary 解析用
        /// SFシリーズ
        /// </summary>
        /// <param name="data">バイナリデータ</param>
        /// <param name="lineLength">GPS/Graphデータサイズ</param>
        /// <param name="delmitter">区切りコード(2byte)</param>
        /// <param name="offset">表示オフセット</param>
        /// <returns>文字列変換データ</returns>
        private string dumpEpson(byte[] data, int lineLength, byte[] delmitter, int offset, bool endian)
        {
            StringBuilder dumpdata = new StringBuilder();
            string date = $"start {data[0x48]}/{data[0x49]}/{data[0x4a]} {data[0x4b]}:{data[0x4c]}:{data[0x4d]}";
            dumpdata.Append(date);
            date = $"\nend   {data[0x4e]}/{data[0x4f]}/{data[0x50]} {data[0x51]}:{data[0x52]}:{data[0x53]}";
            dumpdata.Append(date);
            int preAddr = 0;
            //  LapData
            int lapCount = data[0x6c];
            int address = 0x80;
            string buf = $"\n[LapData] count {lapCount}";
            //  LapData Address + title
            buf += "\nAddres";
            for (int j = 0; j < 128; j++)
                buf += " " + j.ToString("X2");
            buf += dispLapDataTitle();
            //  LapData
            for (int i = 0; i < lapCount; i++) {
                buf += "\n" + address.ToString("X6");
                for (int j = 0; j < 128; j++) {
                    buf += " " + data[address + j].ToString("X2");
                }
                buf += " " + dispLapData(data, address);
                address += 128;
            }
            dumpdata.Append(buf);
            // GpsData?
            buf = "\n[GpsData/GraphData]";
            buf += "\n    Address";
            for (int j = 0; j < lineLength; j++)
                buf += " " + j.ToString("X2");
            dumpdata.Append(buf);
            preAddr = address;
            int count = 0;
            buf = "";
            while (address < data.Length - lineLength && count < 10000) {
                if (data[address + offset] == delmitter[0] && data[address + offset  + 1] == delmitter[1]) {
                    if (buf != "")
                        dumpdata.Append($"\n**** {preAddr.ToString("X6")}{buf}");
                    buf = $"\n{count.ToString("D4")} {address.ToString("X6")}";
                    for (int j = 0; j < lineLength; j++) {
                        buf += " " + data[address++].ToString("X2");
                    }
                    count++;
                    dumpdata.Append(buf);
                    buf = dispGpaData(data, address - lineLength, endian);
                    dumpdata.Append(buf);
                    preAddr = address;
                    buf = "";
                } else {
                    buf += " " + data[address++].ToString("X2");
                    //address++;
                    //buf += "*";
                }
            }
            if (buf != "")
                dumpdata.Append($"\n**** {preAddr.ToString("X6")}{buf}");

            return dumpdata.ToString();
        }

        /// <summary>
        /// [LapData]タイトル
        /// </summary>
        /// <returns></returns>
        private string dispLapDataTitle()
        {
            string buf = "  ";
            buf += "No Kind SplitTime Time TimeCentisecond SplitDistance Distance ";
            buf += "Steps Calorie AescentAltitude DescentAltitude SpeedAve PaceAve ";
            buf += "PitchAve StrideAve EndPoint";

            return buf;
        }

        /// <summary>
        /// [LapData]表示
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private string dispLapData(byte[] data, int pos)
        {
            string buf = "  ";
            buf += $"{BitConverter.ToInt16(data, pos)} ";               //  No 
            buf += $"{BitConverter.ToInt16(data, pos + 0x02)}    ";     //  Kind 
            int splitime = data[pos + 0x04] * 60;
                splitime = (splitime + data[pos + 0x05]) * 60;
                splitime = (splitime + data[pos + 0x06]) * 100;
                splitime = splitime + data[pos + 0x07];
            buf += $"{splitime.ToString("D6")}    ";                    //  LapSplitTime 
            int time = data[pos + 0x08] * 60;
                time = (time + data[pos + 0x09]) * 60;
                time = time + data[pos + 0x0A];
            buf += $"{time.ToString("D4")} ";                           //  LapTime 
            buf += $"{data[pos + 0x0B].ToString("D2")}              ";  //  TimeCentisecond 
            buf += $"{BitConverter.ToInt16(data, pos + 0x0C).ToString("D5")}         ";     //  SplitDistance 
            buf += $"{BitConverter.ToInt16(data, pos + 0x10).ToString("D5")}    ";  //  Distance 
            buf += $"{BitConverter.ToInt16(data, pos + 0x14).ToString("D4")}  ";    //  Steps 
            buf += $"{BitConverter.ToInt16(data, pos + 0x18).ToString("D3")}     "; //  Calorie 
            buf += $"{BitConverter.ToInt16(data, pos + 0x1A).ToString("D3")}             "; //  AescentAltitude 
            buf += $"{BitConverter.ToInt16(data, pos + 0x1C).ToString("D3")}             "; //  DescentAltitude
            buf += $"{BitConverter.ToInt16(data, pos + 0x1E).ToString("D5")}    ";  //  SpeedAve
            int pace = data[pos + 0x22] * 60;
                pace = pace + data[pos + 0x23];
            buf += $"{pace.ToString("D4")}    ";                                    //  PaceAve 
            buf += $"{BitConverter.ToInt16(data, pos + 0x2A).ToString("D3")}      "; //  PitchAve 
            buf += $"{BitConverter.ToInt16(data, pos + 0x2E).ToString("D5")}    ";  //  StrideAve 

            return buf;
        }

        /// <summary>
        /// [GraphData/GpsData]表示
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pos"></param>
        /// <param name="littleEndien"></param>
        /// <returns></returns>
        private string dispGpaData(byte[] data, int pos, bool littleEndien = true)
        {
            //pos += 2;
            string buf = " ";
            //buf += $" Time {BitConverter.ToInt16(data,pos + 0x0b).ToString("D4")} ";
            buf += $" Slope {((sbyte)data[pos + 0x03]).ToString("D")} ";
            buf += $" No/Time {toInt(data, 2, pos + 0x0b, littleEndien).ToString("D4")} ";
            buf += $" Latitude {toInt(data, 5, pos + 0x12, littleEndien).ToString("D4")}  ";
            buf += $" Longitude {toInt(data, 5, pos + 0x17, littleEndien).ToString("D4")}  ";
            //buf += $" Latitude {data[pos + 6].ToString("X2")} {data[pos + 7].ToString("X2")} {data[pos + 8].ToString("X2")} {data[pos + 9].ToString("X2")} {data[pos + 10].ToString("X2")} {data[pos + 11].ToString("X2")} ";
            //buf += $" Longitude {data[pos + 13].ToString("X2")} {data[pos + 14].ToString("X2")} {data[pos + 15].ToString("X2")} {data[pos + 16].ToString("X2")} {data[pos + 17].ToString("X2")} {data[pos + 18].ToString("X2")} ";
            //buf += $" Altitude {data[pos + 30].ToString("X2")} {data[pos + 31].ToString("X2")} {data[pos + 32].ToString("X2")} ";


            return buf;
        }

        /// <summary>
        /// 指定位置のデータを文字列に変換
        /// BitConverterはリトルエンディアンで動作
        /// </summary>
        /// <param name="data">byteデータ</param>
        /// <param name="pos">位置</param>
        /// <param name="size">データサイズ</param>
        /// <param name="littleEndien">エンディアン</param>
        /// <returns>変換文字列</returns>
        private string convByteStr(byte[] data, int pos, int size, bool littleEndien = true)
        {
            string form = mDataForm;
            int charCount = mDataDispSize;
            string buf = "";
            byte[] byteData = convByte(data, pos, size, littleEndien);
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
                case "カスタム":
                    buf = toInt(byteData, size).ToString(form);
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
        /// 10進数文字列をbyte配列文字列に変換
        /// </summary>
        /// <param name="str"></param>
        /// <param name="littleEndian"></param>
        /// <returns></returns>
        private string dec2Byte(string str, bool littleEndian = true)
        {
            long val = ylib.longParse(str);
            byte[] bytes = BitConverter.GetBytes(val);
            int n = 0;
            for (n = bytes.Length - 1; 0 < n; n--)
                if (bytes[n] != 0) break;
            string buf = "";
            if (littleEndian) {
                for (int i = 0; i <= n && i < bytes.Length; i++)
                    buf += bytes[i].ToString("X2") + " ";
            } else {
                for (int i = n; 0 <= i; i--)
                    buf += bytes[i].ToString("X2") + " ";
            }
            return 0 < buf.Length ? buf.Remove(buf.Length - 1) : "";
        }

        /// <summary>
        /// byte配列を10進文字列に変換
        /// </summary>
        /// <param name="str"></param>
        /// <param name="endian"></param>
        /// <returns></returns>
        private string byte2Dec(string str, bool endian = true)
        {
            string[] datas = str.Split(' ');
            long ret = 0;
            if (endian) {
                for (int i = datas.Length - 1; 0 <= i; i--) {
                    if (0 < datas[i].Length)
                        ret = ret * 256 + ylib.longHexParse(datas[i]);
                }
            } else {
                for (int i = 0; i < datas.Length; i++) {
                    if (0 < datas[i].Length)
                        ret = ret * 256 + ylib.longHexParse(datas[i]);
                }
            }
            return ret.ToString();
        }

        /// <summary>
        /// byte配列の文字列を逆順にする
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string reverseByte(string str)
        {
            string[] strings = str.Split(' ');
            Array.Reverse(strings);
            string buf = "";
            for (int i = 0; i < strings.Length; i++)
                if (strings[i] != " ") buf += strings[i] + " ";
            return 0 < buf.Length ? buf.Remove(buf.Length - 1) : "";
        }

        /// <summary>
        /// 度ddd.ddd→ddd mm ss (byte列)
        /// 48.921206 → 30 37 10 22 10
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string deg2dms(string data)
        {
            double deg = ylib.doubleParse(data);
            int d = (int)Math.Floor(deg);
            int m = (int)Math.Floor((deg - d) * 60);
            int s = (int)Math.Floor((deg - d - m / 60.0) * 3600);
            return $"{d.ToString("X2")} {m.ToString("X2")} {s.ToString("X2")}";
        }


        private string deg2dmss(string data)
        {
            double deg = ylib.doubleParse(data);
            int d = (int)Math.Floor(deg);
            int m = (int)Math.Floor((deg - d) * 60);
            int s = (int)Math.Floor((deg - d - m / 60.0) * 3600);
            int ss = (int)Math.Floor((deg - d - m / 60.0 - s / 3600.0) * 3600 * 100);
            return $"{d.ToString("X2")} {m.ToString("X2")} {s.ToString("X2")} {ss.ToString("X2")}";
        }


        /// <summary>
        /// 指定サイズのバイトデータを数値に変換
        /// </summary>
        /// <param name="data">バイトデータ</param>
        /// <param name="size">データサイズ</param>
        /// <param name="pos">データの位置</param>
        /// <param name="littleEndien">エンディアン</param>
        /// <returns>数値</returns>
        private ulong toInt(byte[] data, int size, int pos, bool littleEndien = true)
        {
            byte[] ret = new byte[size];
            Array.Copy(data, pos, ret, 0, size);
            //  ビックエンディアンの時は逆順にする
            if (!littleEndien)
                Array.Reverse(ret);
            return toInt(ret, size);
        }

        /// <summary>
        /// 指定サイズのバイトデータを数値に変換
        /// </summary>
        /// <param name="data">バイトデータ</param>
        /// <param name="size"><データサイズ/param>
        /// <returns>数値</returns>
        private ulong toInt(byte[] data, int size)
        {
            ulong n = 0;
            for (int i = size - 1; 0 <= i; i--) {
                n *= 0x100;
                n += data[i];
            }
            return n;
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
            if (!littleEndien)
                Array.Reverse(ret);
            return ret;
        }

        /// <summary>
        /// 単語の検索(空白区切りの複数単語の検出)
        /// </summary>
        private void search()
        {
            List<string> searchWords = getSearchWords(tbSeachText.Text);
            if (searchWords.Count == 0) return;

            tbBinView.SelectionStart += 1;
            tbBinView.SelectionBrush = Brushes.Red;
            int pos = tbBinView.SelectionStart;
            string word = searchWords[0];
            while (0 <= pos && pos < tbBinView.Text.Length) {
                pos = tbBinView.Text.IndexOf(word, pos);
                int ppos = pos;
                if (pos < 0) {
                    break;
                } else {
                    bool find;
                    (find, pos) = searchWord(tbBinView.Text, searchWords, pos);
                    if (find) {
                        tbBinView.Focus();
                        tbBinView.Select(ppos, pos - ppos - 1);
                        string address = getCursorAddress(tbBinView.SelectionStart);
                        tbSearchPos.Text = $"{address}";
                        return;
                    }
                }
                pos += word.Length;
            }
            MessageBox.Show($"{tbSeachText.Text} が見つかりません");
        }

        /// <summary>
        /// 連続複数単語の検索
        /// </summary>
        /// <param name="text">テキスト</param>
        /// <param name="words">単語リスト</param>
        /// <param name="pos">開始位置</param>
        /// <returns>(検出の有無,次の位置)</returns>
        private (bool, int) searchWord(string text, List<string> words, int pos)
        {
            pos = text.IndexOf(words[0], pos);
            string buf = "";
            for (int i = 0; i < words.Count; i++) {
                (buf, pos) = nextWord(text, pos);
                if (buf == "" || buf != words[i]) return (false, pos);
            }
            return (true, pos);
        }

        /// <summary>
        /// 指定位置から次の空白までの文字を抽出(先頭に改行があるものは除く)
        /// </summary>
        /// <param name="text"> テキスト</param>
        /// <param name="pos">開始位置</param>
        /// <returns>(抽出単語,次の位置)</returns>
        private (string, int) nextWord(string text, int pos)
        {
            string buf = "";
            while (0 <= pos && pos < text.Length) {
                int n = text.IndexOf(' ', pos);
                if (n == pos) {
                    pos++;
                } else if (0 < n) {
                    buf = text.Substring(pos, n - pos);
                    int m = buf.IndexOf('\n');
                    if (m < 0) {
                        buf = text.Substring(pos, n - pos);
                        pos += buf.Length + 1;
                        break;
                    } else if (m == 0) {
                        pos = n + 1;
                    } else {
                        buf = text.Substring(pos, m);
                        pos = n + 1;
                        break;
                    }
                } else {
                    buf = text.Substring(pos);
                    pos += buf.Length + 1;
                    break;
                }
            }
            return (buf.Trim(), pos);
        }

        /// <summary>
        /// 検索文字列を単語リストに変換
        /// </summary>
        /// <param name="word">検索文字</param>
        /// <returns>単語リスト</returns>
        private List<string> getSearchWords(string word)
        {
            word = word.Trim();
            string[] words = word.Split(' ');
            return words.ToList();
        }

        /// <summary>
        /// カーソル位置の行と列のアドレスを取得する
        /// </summary>
        /// <returns>(行,列)</returns>
        private string getCursorAddress(int cursorPosition)
        {
            int lineIndex = tbBinView.Text.LastIndexOf('\n', cursorPosition);
            if (lineIndex < 0) return "";
            int n = tbBinView.Text.IndexOf(':',lineIndex);
            int size = n - lineIndex - 1;
            string address = "";
            if (0 < n && 0 < size && size < 10 && n + size < tbBinView.Text.Length)
                address = tbBinView.Text.Substring(lineIndex + 1, size).TrimStart('0');
            else
                return "";
            int colIndex = cursorPosition - lineIndex - 1;
            n = tbBinView.Text.IndexOf(' ', colIndex);
            size = n - colIndex;
            if (0 < n && 0 < size && n + size < tbBinView.Text.Length)
                address += "," + tbBinView.Text.Substring(colIndex, size).TrimStart('0');
            return address;
        }

        /// <summary>
        /// メモ機能
        /// </summary>
        private void memo()
        {
            string path = getMemoFileName(cbFileSelect.Text);
            if (path == "") return;
            if (mMemoDlg != null)
                mMemoDlg.Close();
            mMemoDlg = new InputBox();
            //mMemoDlg.Topmost = true;
            mMemoDlg.mFontFamily = "MS Gothic";
            mMemoDlg.mFilePath = path;
            mMemoDlg.mMultiLine = true;
            mMemoDlg.mTextRapping = false;
            mMemoDlg.Title = $"めも [{Path.GetFileName(path)}]";
            mMemoDlg.mCalcMenu = true;
            mMemoDlg.mHexCalcMenu = true;
            if (File.Exists(path)) {
                mMemoDlg.mEditText = ylib.loadTextFile(path);
            } else {
                mMemoDlg.mEditText = getFileInfo(cbFileSelect.Text);
            }
            mMemoDlg.mCallBackOn = true;
            mMemoDlg.callback = setMemoText;
            mMemoDlg.Show();
        }

        /// <summary>
        /// 図面のメモデータをmParaに設定する(CallBack)
        /// </summary>
        public void setMemoText()
        {
            if (mMemoDlg != null && mMemoDlg.IsVisible) {
                mMemoDlg.updateData();
                if (mMemoDlg.mEditText.Length == 0) return;
                string path = mMemoDlg.mFilePath;
                if (File.Exists(path)) {
                    ylib.saveTextFile(path, mMemoDlg.mEditText);
                } else {
                    if (MessageBox.Show("メモを保存しますか","確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        ylib.saveTextFile(path, mMemoDlg.mEditText);
                }
            }
        }

        /// <summary>
        /// メモデータのファイル名の作成
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string getMemoFileName(string filePath)
        {
            if (filePath == "") return "";
            string path = Path.GetFileNameWithoutExtension(filePath) + ".memo";
            return Path.Combine(Path.GetDirectoryName(filePath), path);
        }

        /// <summary>
        /// ファイル情報の取得
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string getFileInfo(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            string buf = "ファイル名 : " + fileInfo.Name;
            buf += "\n作成日時 : " + fileInfo.CreationTime.ToString("G");
            buf += "\n更新日時 : " + fileInfo.LastWriteTime.ToString("G");
            buf += "\nサイズ　 : " + fileInfo.Length.ToString("N");
            buf += "\n";
            return buf;
        }

        /// <summary>
        /// バイナリデータの読込
        /// </summary>
        /// <param name="path"></param>
        private void loadData(string path)
        {
            if (0 < path.Length && File.Exists(path)) {
                mBinData = ylib.loadBinData(path);
                if (mBinData != null) {
                    tbFileProp.Text = "size = " + mBinData.Length.ToString();
                    tbBinView.Text = dumpData(mBinData);
                } else {
                    tbBinView.Text = "";
                }
            }
        }

        /// <summary>
        /// ファイルリストを読み込む
        /// </summary>
        /// <param name="path">ファイルパス</param>
        private void loadFileList(string path)
        {
            if (File.Exists(path)) {
                List<string> fileList = ylib.loadListData(mFileListName);
                if (0 < fileList.Count) {
                    cbFileSelect.Items.Clear();
                    foreach (string file in fileList)
                        if (File.Exists(file) && !cbFileSelect.Items.Contains(file))
                            cbFileSelect.Items.Add(file);
                }
            }
        }

        /// <summary>
        /// ファイルリストを保存する
        /// </summary>
        /// <param name="path">ファイル名</param>
        private void saveFileList(string path)
        {
            if (cbFileSelect.Items.Count > 0) {
                //  ファイルリスト保存
                List<string> fileList = new List<string>();
                for (int i = 0; i < cbFileSelect.Items.Count; i++)
                    fileList.Add(cbFileSelect.Items[i].ToString());
                ylib.saveListData(path, fileList);
            }
        }
    }
}

