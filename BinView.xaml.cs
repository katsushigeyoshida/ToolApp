using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
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
            "byte", "ascii", "int8", "int16", "int32", "int64", "float", "double", "日時", "時間", "時間2", "度1", "度2", "度3" };
        private int[] mDataStep = {                             //  データサイズ
             1,      1,       1,      2,       4,       8,       4,       8,        4,      4,      4,       4,     4,     4    };
        private string[] mDataForm = {                          //  表示フォーマット
            "X2",   "X1",    "D3",   "D5",    "D10",   "D20",   "D19",   "F6",     "F6",   "F6",   "F6",    "F6",  "F6",  "F6" };
        private int[] mCharCount = {                            //  表示文字数
             2,      1,       3,      5,       10,      20,      14,      22,       19,     12,     9,      22,    22,    12 };
        private List<double> mFontSizes = new List<double>() {
                 8, 9, 10, 11, 11.5, 12, 13, 14, 15, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72
            };
        public InputBox mMemoDlg = null;                        //  メモダイヤログ

        private YLib ylib = new YLib();

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
        /// データの再表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btLoad_Click(object sender, RoutedEventArgs e)
        {
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
                if (cbFileSelect.Items.Contains(path))
                    cbFileSelect.Items.Remove(path);
                cbFileSelect.Items.Insert(0, path);
                cbFileSelect.SelectedIndex = 0;
            } else {
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
        /// バイナリデータの表示
        /// </summary>
        /// <param name="data">バイナリデータ</param>
        private string dumpData(byte[] data)
        {
            if (data == null || data.Length == 0) return "";

            int start = ylib.intParse(tbStart.Text.ToString());         //  開始位置(byte)
            int typeSize = mDataStep[cbDataType.SelectedIndex];         //  単位データサイズ
            int rowSize = ylib.intParse(tbColCount.Text.ToString());    //  １行のバイト数
            int charCount = mCharCount[cbDataType.SelectedIndex];
            string colForm = $"X{charCount}";                           //  列タイトルのフォーマット
            string sep = new string('-', charCount);                    //  セパレータ
            bool endian = cbEndian.IsChecked == true;                   //  リトルエンディアン

            StringBuilder buf = new StringBuilder();
            //  桁数タイトル
            buf.Append(new string(' ', 7));
            for (int i = 0; i < rowSize; i += typeSize) {
                if (i % 8 == 0)
                    buf.Append(" ");
                if (1 < charCount)
                    buf.Append($"{i.ToString(colForm)} ");
                else
                    buf.Append($"{i.ToString(colForm).Substring(i.ToString(colForm).Length - 1, 1)}");
            }
            //  セパレータ
            buf.Append("\n" + new string(' ', 7));
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
                buf.Append($"\n{row:X6}:");
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
            return buf.ToString();
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
    }
}

