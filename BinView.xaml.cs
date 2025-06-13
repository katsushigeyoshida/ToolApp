using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfLib;

namespace ToolApp
{

    class DataType
    {
        public string Name { get; set; }        //  データ型名
        public int ByteSize { get; set; }       //  データのバイト数
        public int DispSize { get; set; }       //  表示桁数
        public string DataForm { get; set; }    //  表示書式("X2","D4","F"...)
        public int Offset { get; set; }         //  対象列(0 > 全列対象)
        public string Express { get; set; }     //  計算式(f(@))

        private YLib ylib = new YLib();

        public DataType(string name, int byteSize, string dataForm, int dispSize) {
            Name = name;
            ByteSize = byteSize;
            DispSize = dispSize;
            DataForm = dataForm;
            Offset   = -1;
            Express  = "";
        }

        public DataType(string name, int byteSize, string dataForm, int dispSize, int offset, string express)
        {
            Name = name;
            ByteSize = byteSize;
            DispSize = dispSize;
            DataForm = dataForm;
            Offset   = offset;
            Express  = express;
        }

        public DataType(string str)
        {
            string[] strings = str.Split(',');
            Name = "カスタム";
            if (strings.Length == 1) {
                ByteSize = Math.Min(ylib.intParse(strings[0]), 8);
                DispSize = ByteSize < 5 ? ByteSize * 2 + 2 :
                    ByteSize < 8 ? ByteSize * 2 + 3 : ByteSize * 2 + 4;
                DataForm = $"D{DispSize}";
            } else if (strings.Length == 2) {
            } else if (strings.Length == 3) {
            } else if (strings.Length == 4) {
            } else if (strings.Length == 5) {
                Offset = ylib.intParse(strings[0]);
                ByteSize = ylib.intParse(strings[1]);
                DataForm = strings[2];
                DispSize = ylib.intParse(strings[3]);
                Express = strings[4];
            }
        }

        public DataType copy()
        {
            return new DataType(Name, ByteSize, DataForm, DispSize, Offset, Express);
        }

        public void setDataType(DataType dataType)
        {
            Name     = dataType.Name;
            ByteSize = dataType.ByteSize;
            DispSize = dataType.DispSize;
            DataForm = dataType.DataForm;
            Offset   = dataType.Offset;
            Express  = dataType.Express;
        }

    }


    /// <summary>
    /// BinView.xaml の相互作用ロジック
    /// </summary>
    public partial class BinView : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅

        private List<DataType> mDataTypes = new List<DataType>() {
            new DataType("byte",          1, "X2",  2),
            new DataType("ascii",         1, "X1",  1),
            new DataType("int8",          1, "D",   4),
            new DataType("uint8",         1, "D",   3),
            new DataType("int16",         2, "D",   6),
            new DataType("uint16",        2, "D",   5),
            new DataType("int32",         4, "D",  11),
            new DataType("uint32",        4, "D",  10),
            new DataType("int64",         8, "D",  21),
            new DataType("uint64",        8, "D",  20),
            new DataType("float",         4, "D19",14),
            new DataType("double",        8, "F6", 22),
            new DataType("カスタム",      1, "X2",  2),
            new DataType("日時",          4, "F6", 19),
            new DataType("時間(hhx100)",  4, "F6", 12),
            new DataType("時間(ssss)",    4, "F6",  9),
            new DataType("度(ssss)",      4, "F6", 22),
            new DataType("度(semicircle)",4, "F6", 22),
            new DataType("度(mmx100000)", 4, "F6", 12),
        };
        private string mFileListName = "BinFieList.csv";        //  ファイル名リスト保存ファイ露命
        private byte[] mBinData;                                //  読込データ
        private string[] mDataStruct = {                        //  構造化処理名
            "なし", "EpsonSf", "EpsonJ" };
        private List<double> mFontSizes = new List<double>() {
                 8, 9, 10, 11, 11.5, 12, 13, 14, 15, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72
            };

        private StatisticDlg mStatisticDlg;                             //  統計ダイヤログ
        private InputBox mMemoDlg = null;                               //  メモダイヤログ
        private string mCustomDataPath = "CustomDataList.csv";
        private List<string[]> mCustomDataList = new List<string[]>();  //  カスタムデータリスト(title,dataType1,dataType2..)
        private List<DataType> mCustomData = new List<DataType>();      //  カスタム対応列

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
            cbDataStruct.ItemsSource = mDataStruct;
            cbDataStruct.SelectedIndex = 0;
            cbDataType.ItemsSource = mDataTypes.Select(p => p.Name);
            cbDataType.SelectedIndex = 0;
            cbFontSize.ItemsSource = mFontSizes;
            cbFontSize.SelectedIndex = mFontSizes.FindIndex(p => p == tbBinView.FontSize);
            loadCustomDataList(mCustomDataPath);
            cbCustomData.ItemsSource = mCustomDataList.Select(p => p[0]).ToArray();

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
            saveCustomDataList(mCustomDataPath);
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
                tbStart.Text = "0";
                tbEnd.Text = "";
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
            if (0 <= cbDataType.SelectedIndex && 0 <= cbDataStruct.SelectedIndex) {
                setDataTypePara(cbDataType.SelectedIndex, cbDataStruct.SelectedIndex);
                tbBinView.Text = dumpData(mBinData, mDataTypes[cbDataType.SelectedIndex]);
            }
        }

        /// <summary>
        /// エンディアン変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbEndian_Click(object sender, RoutedEventArgs e)
        {
            tbBinView.Text = dumpData(mBinData, mDataTypes[cbDataType.SelectedIndex]);
        }

        private void cbCustomData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void cbCustomData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        /// <summary>
        /// 表示文字サイズの設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFontSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            tbBinView.FontSize = ylib.doubleParse(cbFontSize.Items[cbFontSize.SelectedIndex].ToString());
            tbBinView.Text = dumpData(mBinData, mDataTypes[cbDataType.SelectedIndex]);
        }


        /// <summary>
        /// [再表示]データの再表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btLoad_Click(object sender, RoutedEventArgs e)
        {
            if (0 <= cbDataType.SelectedIndex && 0 <= cbDataStruct.SelectedIndex) {
                setDataTypePara(cbDataType.SelectedIndex, cbDataStruct.SelectedIndex, false);
                tbBinView.Text = dumpData(mBinData, mDataTypes[cbDataType.SelectedIndex]);
            }
        }

        /// <summary>
        /// [先頭]ボタン(先頭から検索)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btTopSearch_Click(object sender, RoutedEventArgs e)
        {
            tbBinView.SelectionStart = 0;
            search(tbSeachText.Text);
        }

        /// <summary>
        /// [次]ボタン (カーソル位置から検索)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btNextSearch_Click(object sender, RoutedEventArgs e)
        {
            search(tbSeachText.Text);
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
        /// [統計]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btStatistic_Click(object sender, RoutedEventArgs e)
        {
            int index = cbFileSelect.SelectedIndex;
            if (index < 0) return;
            string path = cbFileSelect.Items[index].ToString();

            int startPos = (int)ycalc.expression(tbStart.Text.ToString());      //  開始位置(byte)
            int endPos = (int)ycalc.expression(tbEnd.Text.ToString());          //  終了位置
            if (startPos >= endPos || mBinData.Length < endPos)
                endPos = mBinData.Length;
            if (mStatisticDlg != null)
                mStatisticDlg.Close();
            mStatisticDlg = new StatisticDlg();
            mStatisticDlg.Title = Path.GetFileName(path);
            mStatisticDlg.mData = mBinData;
            mStatisticDlg.mStartPos = startPos;
            mStatisticDlg.mEndPos = endPos;
            mStatisticDlg.Show();
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
                tbSeachText.Text = ylib.dec2Byte(tbSeachText.Text, cbEndian.IsChecked == true);
            } else if (menuItem.Name.CompareTo("tbByte2DecMenu") == 0) {
                //  byte列 → Dec
                tbSeachText.Text = ylib.byte2Dec(tbSeachText.Text, cbEndian.IsChecked == true);
            } else if (menuItem.Name.CompareTo("tbReverseByteMenu") == 0) {
                //  Byte反転
                tbSeachText.Text = ylib.reverseByte(tbSeachText.Text);
            } else if (menuItem.Name.CompareTo("tbDeg2dmsMenu") == 0) {
                //  度ddd.ddd → ddd mm ss(byte)
                tbSeachText.Text = deg2dms(tbSeachText.Text);
            } else if (menuItem.Name.CompareTo("tbDeg2dmssMenu") == 0) {
                //  度ddd.ddd → ddd mm ssss (byte)
                tbSeachText.Text = deg2dmss(tbSeachText.Text);
            } else if (menuItem.Name.CompareTo("tbSec2hmsMenu") == 0) {
                //  秒ssss → ddd mm ss (byte)
                tbSeachText.Text = sec2hms(tbSeachText.Text);
            } else if (menuItem.Name.CompareTo("tbM2kkmmMenu") == 0) {
                //  m mmm → kk mmm (byte)
                tbSeachText.Text = mmm2kkmm(tbSeachText.Text);
            }
        }

        /// <summary>
        /// カスタムデータメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbCustomDataMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("cbCustomDataAddMenu") == 0) {
                //  追加
                addCustomData();
            } else if (menuItem.Name.CompareTo("cbCustomDataEditMenu") == 0) {
                //  編集
                editCustomData();
            } else if (menuItem.Name.CompareTo("cbCustomDataRemoveMenu") == 0) {
                //  削除
                remoceCustomData();
            }
        }

        /// <summary>
        /// カスタムデータ追加
        /// </summary>
        private void addCustomData()
        {
            InputBox dlg = new InputBox();
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.mMultiLine = true;
            dlg.mWindowSizeOutSet = true;
            dlg.mEditText = "title\noffset,ByteSize,DataForm,DispSize,Express(f(@))";
            if (dlg.ShowDialog() == true) {
                string[] buf = dlg.mEditText.Split('\n');
                for (int i = 0; i < buf.Length; i++)
                    buf[i] = buf[i].Replace("\n", "");
                int n = mCustomDataList.FindIndex(p => p[0] == buf[0]);
                if (0 <= n)
                    mCustomDataList.RemoveAt(n);
                mCustomDataList.Insert(0, buf);
                cbCustomData.ItemsSource = mCustomDataList.Select(p => p[0]).ToArray();
            }
        }

        /// <summary>
        /// カスタムデータの編集
        /// </summary>
        private void editCustomData()
        {
            int index = cbCustomData.SelectedIndex;
            if (0 <= index) {
                InputBox dlg = new InputBox();
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dlg.mMultiLine = true;
                dlg.mWindowSizeOutSet = true;
                dlg.mEditText = string.Join("\n", mCustomDataList[index]);
                if (dlg.ShowDialog() == true) {
                    string[] buf = dlg.mEditText.Split('\n');
                    for (int i = 0; i < buf.Length; i++)
                        buf[i] = buf[i].Replace("\n", "");
                    int n = mCustomDataList.FindIndex(p => p[0] == buf[0]);
                    mCustomDataList.RemoveAt(index);
                    mCustomDataList.Insert(0, buf);
                    cbCustomData.ItemsSource = mCustomDataList.Select(p => p[0]).ToArray();
                }
            }
        }

        /// <summary>
        /// カスタムデータの削除
        /// </summary>
        private void remoceCustomData()
        {
            int index = cbCustomData.SelectedIndex;
            if (0 <= index) {
                mCustomDataList.RemoveAt(index);
                cbCustomData.ItemsSource = mCustomDataList.Select(p => p[0]).ToArray();
            }
        }

        /// <summary>
        /// データタイプごとにパラメータの設定
        /// </summary>
        /// <param name="typeindex">データ型</param>
        /// <param name="structIndex">データ構造</param>
        /// <param name="first">初期化(開始位置,列サイズ)</param>
        private void setDataTypePara(int typeindex, int structIndex, bool first = true)
        {
            //tbDataTypeSize.IsEnabled = false;                   //  カスタムのデータサイズ
            if (mDataTypes[typeindex].Name == "カスタム" && 0 <= cbCustomData.SelectedIndex) {
                mCustomData.AddRange(string2DataTypeList(mCustomDataList[cbCustomData.SelectedIndex]));
            } else {
                mCustomData.Clear();
            }
            if (first && mDataStruct[structIndex] == "EpsonSf") {
                tbColCount.Text = "57";
            } else if (first && mDataStruct[structIndex] == "EpsonJ") {
                //tbColCount.Text = "53";
                //tbStart.Text = "21";
            }
        }

        /// <summary>
        /// カスタムデータの文字列をDataTypeリストに変換
        /// </summary>
        /// <param name="dataArray">カスタムデータ</param>
        /// <returns>DataType</returns>
        private List<DataType> string2DataTypeList(string[] dataArray)
        {
            List<DataType> dataTypes = new List<DataType>();
            for (int i = 1; i < dataArray.Length; i++)
                dataTypes.Add(new DataType(dataArray[i]));
            return dataTypes;
        }


        /// <summary>
        /// バイナリデータの表示
        /// </summary>
        /// <param name="data">バイナリデータ</param>
        private string dumpData(byte[] data, DataType dataType)
        {
            if (data == null || data.Length == 0) return "";

            int startPos = (int)ycalc.expression(tbStart.Text.ToString());      //  開始位置(byte)
            int endPos = (int)ycalc.expression(tbEnd.Text.ToString());          //  終了位置
            if (startPos >= endPos || data.Length < endPos) {
                endPos = data.Length;
                tbEnd.Text = "0x" + endPos.ToString("X");
            }
            int lineLength = (int)ycalc.expression(tbColCount.Text.ToString()); //  １行のバイト数
            bool endian = cbEndian.IsChecked == true;                           //  リトルエンディアン
            List<byte[]> crbyte = crByte(tbCR.Text);

            string structName = cbDataStruct.Items[cbDataStruct.SelectedIndex].ToString();  //  データ構造名

            if (structName == "EpsonSf") {
                return dumpEpson(data, dataType, startPos, endPos, lineLength, endian);
            //} else if (structName == "EpsonJ") {
                //tbStart.IsEnabled = true;
                //tbColCount.IsEnabled = false;
                //return dumpEpson(data, dataType, startPos, endPos, 53, endian);
            } else {
                tbStart.IsEnabled = true;
                tbColCount.IsEnabled = true;
                return dumpData(data, dataType, startPos, endPos, lineLength, crbyte,  endian);
            }
        }

        /// <summary>
        /// テキストをバイトリストに変換
        /// </summary>
        /// <param name="data">CRテキストコード</param>
        /// <returns>byte配列リスト</returns>
        private List<byte[]> crByte(string crtext)
        {
            if (crtext == "") return null;
            string[] texts = crtext.Split('|');
            List<byte[]> crtexts = new List<byte[]>();
            foreach (string text in texts) {
                string[] crbytetext = text.Split(' ');
                List<byte> crbyte = new List<byte>();
                for (int i = 0; i < crbytetext.Length; i++) {
                    if (crbytetext[i] != "")
                        crbyte.Add((byte)ycalc.expression(crbytetext[i].Trim()));
                }
                crtexts.Add(crbyte.ToArray());
            }
            return crtexts;
        }

        /// <summary>
        /// バイナリデータの表示
        /// </summary>
        /// <param name="data">バイナリデータ</param>
        /// <param name="dataType">データの表示形式</param>
        /// <param name="startPos">表示開始位置</param>
        /// <param name="lineLength">1行の表示バイト数</param>
        /// <param name="endian">エンディアン(true=little)</param>
        /// <returns>表示文字列</returns>
        private string dumpData(byte[] data, DataType dataType, int startPos, int endPos, int lineLength, List<byte[]> crbytes, bool endian)
        {
            StringBuilder buf = new StringBuilder();
            //  桁数タイトル
            buf.Append(new string(' ', 7));
            for (int i = 0; i < lineLength; i += dataType.ByteSize) {
                buf.Append(dispForm(i, dataType, i.ToString($"X{dataType.DispSize}"), true));
            }
            //  セパレータ
            buf.Append("\n" + new string(' ', 7));
            for (int i = 0; i < lineLength; i += dataType.ByteSize) {
                string sep = new string('-', dataType.DispSize);                    //  セパレータ
                buf.Append(dispForm(i, dataType, sep, true));
            }
            //  データ
            int address = startPos;
            while (address < data.Length && address < endPos) {
                buf.Append($"\n{address:X6}:");
                for (int i = 0; i < lineLength && address + i < data.Length - dataType.ByteSize; i += dataType.ByteSize) {
                    if (i != 0 && crcheck(data, address + i, crbytes)) {
                        address += i - lineLength;
                        break;
                    }
                    string strdata = convByteStr(data, address, i, dataType, endian);
                    buf.Append(dispForm(i, dataType, strdata));
                }
                address += lineLength;
            }
            return buf.ToString();
        }

        /// <summary>
        /// 改行コードチェック
        /// </summary>
        /// <param name="data">バイナリデータ</param>
        /// <param name="address">位置</param>
        /// <param name="offset">オフセット</param>
        /// <param name="crbytes">改行コードリスト</param>
        /// <returns></returns>
        private bool crcheck(byte[] data, int address, List<byte[]> crbytes)
        {
            if (crbytes != null) {
                foreach (var crbyte in crbytes)
                    if (crbyte != null && YLib.ByteComp(crbyte, 0, data, address, crbyte.Length))
                        return true;
            }
            return false;
        }

        /// <summary>
        /// Epson GPS Watch Workout Binary 解析用
        /// SFシリーズ
        /// </summary>
        /// <param name="data">バイナリデータ</param>
        /// <param name="dataType">データの表示形式</param>
        /// <param name="startPos">表示表示開始位置</param>
        /// <param name="lineLength">固定長のデータサイズ</param>
        /// <returns>文字列変換データ</returns>
        private string dumpEpson(byte[] data, DataType dataType, int startPos, int endPos, int lineLength, bool endian)
        {
            List<byte[]> crcode = new List<byte[]> { 
                new byte[] { 0x30, 0x10},
                new byte[] { 0x30, 0x20},
                new byte[] { 0x31, 0x10},
                new byte[] { 0x31, 0x20},
            };
            StringBuilder dumpdata = new StringBuilder();
            string colForm = $"X{dataType.DispSize}";                           //  列タイトルのフォーマット

            //  Header
            dumpdata.Append(header(data));

            //  LapData
            int preAddr = 0;
            int lapCount = data[0x6c];
            int address = 0x80;
            string buf = $"\n[LapData] count {lapCount}";

            //  LapData title
            buf += "\n    Address";
            for (int j = 0; j < 128; j += dataType.ByteSize)
                buf += dispForm(j / dataType.ByteSize, dataType, j.ToString(colForm), true);
            buf += dispLapDataTitle();

            while (data[address] == 0xFF)
                address += 64;
            //  LapData
            for (int i = 0; i < lapCount; i++) {
                buf += "\n     " + address.ToString("X6");
                for (int j = 0; j < 128; j++) {
                    string strdata = convByteStr(data, address ,j, dataType, endian);
                    buf += dispForm(j, dataType, strdata);
                }
                buf += " " + dispLapData(data, address);        //  LapDataデータの中身
                address += 128;
            }
            dumpdata.Append(buf);

            // 固定長データ GpsData?
            buf = "\n[GpsData/GraphData]";
            buf += "\n " + dispGpsHeader(data, address);
            buf += "\n    Address";
            for (int j = 0; j < lineLength; j += dataType.ByteSize)
                buf += dispForm(j / dataType.ByteSize, dataType, j.ToString(colForm), true);
            dumpdata.Append(buf);
            preAddr = address;
            int count = 0;
            while (address < data.Length && address < endPos) {
                buf = "";
                int offset = 0;
                for (int i = 0; i < lineLength && address + i < data.Length - dataType.ByteSize; i += dataType.ByteSize) {
                    if (i != 0 && crcheck(data, address + i, crcode)) {
                        offset = i;
                        break;
                    }
                    string strdata = convByteStr(data, address, i, dataType, endian);
                    buf += dispForm(i, dataType, strdata);
                }
                if (crcheck(data, address, crcode)) {
                    dumpdata.Append($"\n{count.ToString("D4")} {address.ToString("X6")}{buf}");
                    buf = dispGpaData(data, address, endian);   //  GpsDataデータの内容
                    dumpdata.Append(buf);
                    count++;
                } else {
                    dumpdata.Append($"\n**** {address.ToString("X6")}{buf}");
                }
                address += offset == 0 ? lineLength : offset;
            }
            if (buf != "")
                dumpdata.Append($"\n**** {preAddr.ToString("X6")}{buf}");

            return dumpdata.ToString();
        }

        /// <summary>
        /// Headerデータ
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string header(byte[] data)
        {
            //  日時その他データ
            string buf = $"start {data[0x48]}/{data[0x49]}/{data[0x4a]} {data[0x4b]}:{data[0x4c]}:{data[0x4d]}";
            buf += $"\nend   {data[0x4e]}/{data[0x4f]}/{data[0x50]} {data[0x51]}:{data[0x52]}:{data[0x53]}";
            buf += $"\nTrainingTime {data[4]}:{data[5]}:{data[6]}.{data[7]}";
            buf += $"\ndistance {ylib.toUInt(data, 4, 8)} steps {ylib.toUInt(data,4,12)}";
            buf += $"\nAltitudeMax {ylib.toInt(data,2,16)} AltitudeMin {ylib.toInt(data,2,18)} AltitudeStart {ylib.toUInt(data,2,20)} AltitudeGoal {ylib.toUInt(data,2,22)}";
            buf += $"\nTotalAscentAltitude {ylib.toUInt(data, 2, 24)} TotalDescentAltitude {ylib.toUInt(data, 2, 26)}";
            buf += $"\nCalone {ylib.toUInt(data, 2, 28)} PitchAve {ylib.toUInt(data, 2, 34)} StrideAve {ylib.toUInt(data, 1, 38)} SpeedAve {ylib.toUInt(data, 4, 44)} PaceAve {data[47]}:{data[48]}:{data[49]}";
            buf += $"\nTimeZoneOffset {ylib.toUInt(data, 2, 86)} DaylightSavingsTime {data[88]}";
            buf += $"\nHeght {ylib.toUInt(data, 2, 92)} weight {ylib.toUInt(data, 2, 94)} Birthday {ylib.toUInt(data,2,110)}/{data[112]}/{data[113]}";
            buf += $"\nLapCount {data[108]}";
            return buf;
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
        /// GPSDataのヘッダ部の表示
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private string dispGpsHeader(byte[] data, int pos)
        {
            string date = $"start {data[pos + 2]}/{data[pos + 3]}/{data[pos + 4]} {data[pos + 5]}:{data[pos + 6]}:{data[pos + 7]}";
            return date;
        }

        private int mLati, mLong, mAlti;

        /// <summary>
        /// [GraphData/GpsData]表示
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pos"></param>
        /// <param name="littleEndien"></param>
        /// <returns></returns>
        private string dispGpaData(byte[] data, int pos, bool littleEndien = true)
        {
            string buf = " ";
            int graphOffset = 0;
            if (data[pos] == 0x30) {
                //  GpsData
                mLati = BitConverter.ToInt32(data, pos + 0x04);
                mLong = BitConverter.ToInt32(data, pos + 0x08);
                mAlti = BitConverter.ToInt32(data, pos + 0x0C);
                if (data[pos + 0x28] == 0x50) graphOffset = 0x28;   //  GraphData
            } else if (data[pos] == 0x31) {
                //  GpsData
                mLati += BitConverter.ToInt16(data, pos + 0x02);
                mLong += BitConverter.ToInt16(data, pos + 0x04);
                mAlti += BitConverter.ToInt16(data, pos + 0x06);
                if (data[pos + 0x20] == 0x50) graphOffset = 0x20;   //  GraphData
            } else
                return buf;
            buf += $" Latitude  {format((double)mLati / (double)0x400000, "F6", 10)}";
            buf += $" Longitude {format((double)mLong / (double)0x400000, "F6", 10)}";
            buf += $" Altitude  {format((double)mAlti / (double)0xA0, "F2", 5)}";
            //buf += $" [21]Alti? {format(BitConverter.ToInt16(data, pos + 0x21), "D", 6)} ";
            if (0 < graphOffset) {
                pos += graphOffset;
                buf += $" [01]Slope {format((sbyte)data[pos + 0x01], "D", 2)} ";
                buf += $" [04]Speed ? {format(ylib.toUInt(data, 4, pos + 0x04, littleEndien) / 64, "D", 4)} ";
                buf += $" [08]Distance {format(ylib.toUInt(data, 4, pos + 0x08, littleEndien) / 64, "D", 6)} ";
                //buf += $" [09]No/Time? {format(ylib.toUInt(data, 2, pos + 0x09, littleEndien), "D", 4)} ";
                buf += $" [11]Stride ? {format(ylib.toUInt(data, 1, pos + 0x11, littleEndien) / 64, "D", 4)} ";
                buf += $" [12]Pitch ? {format(ylib.toUInt(data, 2, pos + 0x12, littleEndien) / 64, "D", 4)} ";
                buf += $" [1A]Status? {format(ylib.toUInt(data, 1, pos + 0x1a, littleEndien), "D", 4)} ";
            }
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
        private string convByteStr(byte[] data, int pos, int offset, DataType dataType, bool littleEndien = true)
        {
            string buf = "";
            byte[] byteData = ylib.convByte(data, pos + offset, dataType.ByteSize, littleEndien);
            switch (dataType.Name) {
                case "byte": buf = byteData[0].ToString(dataType.DataForm); break;
                case "ascii":
                    buf = buf = 0x20 <= byteData[0] && byteData[0] < 0x7f ?
                        ((char)byteData[0]).ToString() : ".";
                    break;
                case "int8":   buf = format((sbyte)byteData[0], dataType.DataForm, dataType.DispSize); break;
                case "uint8":  buf = format(byteData[0], dataType.DataForm, dataType.DispSize); break;
                case "int16":  buf = format(BitConverter.ToInt16(byteData, 0), dataType.DataForm, dataType.DispSize); break;
                case "uint16": buf = format(BitConverter.ToUInt16(byteData, 0), dataType.DataForm, dataType.DispSize); break;
                case "int32":  buf = format(BitConverter.ToInt32(byteData, 0), dataType.DataForm, dataType.DispSize); break;
                case "uint32": buf = format(BitConverter.ToUInt32(byteData, 0), dataType.DataForm, dataType.DispSize); break;
                case "int64":  buf = format(BitConverter.ToInt64(byteData, 0), dataType.DataForm, dataType.DispSize); break;
                case "uint64": buf = format(BitConverter.ToUInt64(byteData, 0), dataType.DataForm, dataType.DispSize); break;
                case "float":
                    buf = BitConverter.ToSingle(byteData, 0).ToString().PadLeft(dataType.DispSize);
                    break;
                case "double":
                    buf = BitConverter.ToDouble(byteData, 0).ToString().PadLeft(dataType.DispSize);
                    break;
                case "カスタム":
                    buf = byteCustomStr(data, pos, offset, dataType, littleEndien);
                    //buf = toUInt(byteData, dataType.ByteSize).ToString(dataType.DataForm);
                    break;
                case "日時":                      //  1991.1.1からの秒数
                    var baseDate = new DateTime(1989, 12, 31, 0, 0, 0);
                    DateTime dt = baseDate.AddSeconds(BitConverter.ToUInt32(byteData, 0));
                    buf = dt.ToString("yyyy/MM/dd HH:mm:ss");
                    break;
                case "時間(hhx100)":                 //  時間 hhmmss.ss
                    buf = ((double)BitConverter.ToUInt32(byteData, 0) / 100).ToString().PadLeft(dataType.DispSize);
                    break;
                case "時間(ssss)":                  //  秒 hhmmss.ss
                    int seconds = (int)BitConverter.ToUInt32(byteData, 0);
                    TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                    buf = timeSpan.ToString(@"hh\:mm\:ss");
                    break;
                case "度(ssss)":                     //  座標データ(秒→度)
                    buf = ((double)BitConverter.ToUInt32(byteData, 0) / 3600).ToString().PadLeft(dataType.DispSize);
                    break;
                case "度(semicircle)":               //  座標データ(緯度・経度(semicircle→度))
                    buf = ((double)BitConverter.ToUInt32(byteData, 0) / Math.Pow(2, 31) * 180).ToString().PadLeft(dataType.DispSize);
                    break;
                case "度(mmx100000)":                //  座標データ(分→分)
                    buf = ((double)BitConverter.ToUInt32(byteData, 0) / 100000).ToString().PadLeft(dataType.DispSize);
                    break;
            }
            return buf;
        }

        /// <summary>
        /// 表示データ
        /// </summary>
        /// <param name="offset">位置</param>
        /// <param name="dataType">表示設定</param>
        /// <param name="data">データ文字列</param>
        /// <param name="title">タイトルの2重表示</param>
        /// <returns>表示文字列</returns>
        private string dispForm(int offset, DataType dataType, string data, bool title = false)
        {
            string buf = "";
            if (dataType.Name == "カスタム" && 0 < mCustomData.Count) {
                if (mCustomData[0].Offset < 0 || mCustomData[0].Offset == offset) {
                    if (offset % 8 == 0) buf += " ";
                    buf += dispForm(mCustomData[0], data);
                    if (title) {
                        if (offset % 8 == 0) buf += " ";
                        buf += dispForm(dataType, data);
                    }
                    return buf;
                }
            }
            if (offset % 8 == 0) buf += " ";
            buf += dispForm(dataType, data);
            return buf;
        }

        /// <summary>
        /// 表示桁数設定
        /// </summary>
        /// <param name="dataType">表示データ設定</param>
        /// <param name="data">表示文字列</param>
        /// <returns></returns>
        private string dispForm(DataType dataType, string data)
        {
            string buf = "";
            if (dataType.DispSize == 1) data = data.Substring(data.Length - 1, 1);
            buf += data.PadLeft(dataType.DispSize);
            if (1 < dataType.DispSize) buf += " ";
            return buf;
        }

        /// <summary>
        /// カスタム表示(カスタム表示 + 通常表示)
        /// </summary>
        /// <param name="data">byteデータ</param>
        /// <param name="address">データ位置</param>
        /// <param name="offset">オフセット</param>
        /// <param name="dataType">表示データ設定パラメータ</param>
        /// <param name="littleEndien">エンディアン</param>
        /// <returns>表示文字列</returns>
        private string byteCustomStr(byte[] data, int address, int offset, DataType dataType, bool littleEndien)
        {
            string buf = "";
            byte[] byteData;
            if (dataType.Name == "カスタム" && 0 < mCustomData.Count) {
                if (mCustomData[0].Offset < 0 || mCustomData[0].Offset == offset) {
                    byteData = ylib.convByte(data, address + offset, mCustomData[0].ByteSize, littleEndien);
                    buf += byteCustomStr(byteData, mCustomData[0].copy());
                    buf += " ";
                }
            }
            byteData = ylib.convByte(data, address + offset, dataType.ByteSize, littleEndien);
            buf += ylib.toUInt(byteData, dataType.ByteSize).ToString(dataType.DataForm);
            return buf;
        }

        /// <summary>
        /// byteデータのカスタム表示変換
        /// </summary>
        /// <param name="byteData">byteデータ</param>
        /// <param name="dataType">表示データ設定パラメータ</param>
        /// <returns>表示文字列</returns>
        private string byteCustomStr(byte[] byteData, DataType dataType)
        {
            long n = ylib.toInt(byteData, dataType.ByteSize);
            string express = dataType.Express.Replace("@", n.ToString());
            long res = (long)ycalc.expression(express);
            return format(res, dataType.DataForm, dataType.DispSize);
        }

        /// <summary>
        /// 数値のフォーマット(固定長右詰め)
        /// </summary>
        /// <param name="val">値</param>
        /// <param name="type">書式記号("X","D","F")</param>
        /// <param name="digit">表示桁数(符号を含む)</param>
        /// <returns>文字列</returns>
        private string format<T>(T val, string type, int digit)
        {
            string form = "{0," + digit + ":" + type + "}";
            return string.Format(form, val);
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

        /// <summary>
        /// 度ddd.ddd→ddd mm ss ss(byte列)
        /// 48.921206 → 30 37 10 22 10
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
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
        /// 秒ssss → hh mm ss (byte)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string sec2hms(string data)
        {
            double sec = ylib.doubleParse(data);
            int h = (int)Math.Floor(sec / 3600.0);
            int m = (int)(Math.Floor(sec - h * 3600.0) / 60.0);
            int s = (int)Math.Floor(sec - h * 3600.0 - m * 60.0);
            return $"{h.ToString("X2")} {m.ToString("X2")} {s.ToString("X2")}";
        }

        /// <summary>
        /// メートル mmm → kk mm mm (byte列)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string mmm2kkmm(string data)
        {
            double mmm = ylib.doubleParse(data);
            int k = (int)Math.Floor(mmm / 1000.0);
            int m = (int)Math.Floor(mmm - k * 1000.0);
            return ylib.long2byteStr(k) + " " + ylib.long2byteStr(m) + (m < 256 ? " 00" : "");
        }

        /// <summary>
        /// 単語の検索(空白区切りの複数単語の検出)
        /// </summary>
        private void search(string text)
        {
            List<string> searchWords = getSearchWords(text);
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
            int npos = text.IndexOf(words[0], pos);
            string buf = "";
            for (int i = 0; i < words.Count; i++) {
                (buf, npos) = nextWord(text, npos);
                if (buf == "" || buf != words[i]) return (false, pos);
            }
            return (true, npos);
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
            mMemoDlg.mFileSelectMenu = true;
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
            buf += "\nサイズ　 : " + fileInfo.Length.ToString("N0");
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
                    tbFileProp.Text = "size = " + mBinData.Length.ToString("N0");
                    tbBinView.Text = dumpData(mBinData, mDataTypes[cbDataType.SelectedIndex]);
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

        /// <summary>
        /// カスタムデータの読込
        /// </summary>
        /// <param name="path"></param>
        private void loadCustomDataList(string path)
        {
            if (File.Exists (path))
                mCustomDataList = ylib.loadCsvData(path);
        }

        /// <summary>
        /// カスタムデータの保存
        /// </summary>
        /// <param name="path"></param>
        private void saveCustomDataList(string path)
        {
            if (mCustomDataList != null)
                ylib.saveCsvData(path, mCustomDataList);
        }
    }
}

