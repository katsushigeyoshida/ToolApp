using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfLib;

namespace ToolApp
{


    /// <summary>
    /// DiffFolder.xaml の相互作用ロジック
    /// </summary>
    public partial class DiffFolder : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅

        private DirectoryDiff mDiffFolder;
        private List<DiffFile> mDiffFileList;
        private List<string[]> mFolderTitleList = new List<string[]>();
        private List<string> mTitleListFormat = new List<string>() {
            "Title", "SrcFolder", "DestFolder", "TargetFile", "ExceptFile", "ExceptFolder",
        };
        private string[] mCurFolderTitleList;
        private string mTitleListPath = "FolderTitleList.csv";
        private int mMaxTitleListCount = 20;                              //  最大保存タイトル数
        private string mDiffTool = "";

        private YLib ylib = new YLib();


        public DiffFolder()
        {
            InitializeComponent();

            cbHachChk.IsChecked = true;
            rbDiffFile.IsChecked = true;
            loadTitleList(mTitleListPath);
            if (mFolderTitleList != null) {
                cbFolderTitle.ItemsSource = mFolderTitleList.ConvertAll(x => x[0]);
            }
            WindowFormLoad();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cbFolderTitle.SelectedIndex = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveTitleList(mTitleListPath);
            WindowFormSave();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.DiffFolderWindowWidth < 100 ||
                Properties.Settings.Default.DiffFolderWindowHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.DiffFolderWindowHeight) {
                Properties.Settings.Default.DiffFolderWindowWidth = mWindowWidth;
                Properties.Settings.Default.DiffFolderWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.DiffFolderWindowTop;
                Left = Properties.Settings.Default.DiffFolderWindowLeft;
                Width = Properties.Settings.Default.DiffFolderWindowWidth;
                Height = Properties.Settings.Default.DiffFolderWindowHeight;
            }
            mDiffTool = Properties.Settings.Default.DiffTool;
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            Properties.Settings.Default.DiffTool = mDiffTool;
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.DiffFolderWindowTop = Top;
            Properties.Settings.Default.DiffFolderWindowLeft = Left;
            Properties.Settings.Default.DiffFolderWindowWidth = Width;
            Properties.Settings.Default.DiffFolderWindowHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 比較タイトル選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFolderTitle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= cbFolderTitle.SelectedIndex) {
                tbSrcFolder.Text = mFolderTitleList[cbFolderTitle.SelectedIndex][1];
                tbDstFolder.Text = mFolderTitleList[cbFolderTitle.SelectedIndex][2];
                tbTargetFile.Text = mFolderTitleList[cbFolderTitle.SelectedIndex][3];
                tbExceptFile.Text = mFolderTitleList[cbFolderTitle.SelectedIndex][4];
                tbExceptFolder.Text = mFolderTitleList[cbFolderTitle.SelectedIndex][5];
            }
        }

        /// <summary>
        /// [比較元フォルダマウスダブルクリック]フォルダ選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbSrcFolder_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string srcFlder = ylib.folderSelect(tbSrcFolder.Text);
            if (srcFlder != null && 0 < srcFlder.Length) {
                tbSrcFolder.Text = srcFlder;
            }
        }

        /// <summary>
        /// [比較先フォルダマウスダブルクリック]フォルダ選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbDstFolder_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string dstFlder = ylib.folderSelect(tbDstFolder.Text);
            if (dstFlder != null && 0 < dstFlder.Length) {
                tbDstFolder.Text = dstFlder;
            }
        }

        /// <summary>
        /// [リストダブルクリック]ファイル比較
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgDiffFolder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int index = dgDiffFolder.SelectedIndex;
            if (0 <= index && 0 < mDiffTool.Length) {
                DiffFile fileData = (DiffFile)dgDiffFolder.Items[index];
                string srcPath = fileData.getPath(mCurFolderTitleList[1]);
                string destPath = fileData.getPath(mCurFolderTitleList[2]);
                ylib.processStart(mDiffTool, $"\"{srcPath}\" \"{destPath}\"");
            }
        }

        /// <summary>
        /// [差異/全ファイル切り替え]ラジオボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbDiffFile_Click(object sender, RoutedEventArgs e)
        {
            dispDiffFolder();
        }

        /// <summary>
        /// コンテキストメニュー選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("dgFileSelectMenu") == 0) {
                setDiffTool();
            }
        }

        /// <summary>
        /// [比較]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Name.CompareTo("btComper") == 0) {
                //  フォルダ比較
                if (Directory.Exists(tbSrcFolder.Text) || Directory.Exists(tbDstFolder.Text)) {
                    setFoldeTitleList();
                    mCurFolderTitleList = mFolderTitleList[0];
                    Mouse.OverrideCursor = Cursors.Wait;
                    setDiffFolder(mCurFolderTitleList[1], mCurFolderTitleList[2], cbHachChk.IsChecked == true,
                        mCurFolderTitleList[3], mCurFolderTitleList[4], mCurFolderTitleList[5]);
                    Mouse.OverrideCursor = Cursors.Arrow;
                } else {
                    MessageBox.Show("フォルダが存在しません");
                }
            } else if (bt.Name.CompareTo("btRightUpdate") == 0) {
                //  右側へコピー
                selectCopy(mCurFolderTitleList[1], mCurFolderTitleList[2]);
            } else if (bt.Name.CompareTo("btLeftUpdate") == 0) {
                //  左側にコピー
                selectCopy(mCurFolderTitleList[2], mCurFolderTitleList[1]);
            }
        }

        /// <summary>
        /// [終了ボタン]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 選択したファイルのみを更新(コピー)
        /// </summary>
        /// <param name="srcFolder">コピー元</param>
        /// <param name="destFolder">コピー先</param>
        private void selectCopy(string srcFolder, string destFolder)
        {
            IList selItems = dgDiffFolder.SelectedItems;
            int copyType = cbOverWriteForce.IsChecked == true ? 2 : 0;
            if (0 < selItems.Count) {
                pbCopyCount.Minimum = 0;
                pbCopyCount.Maximum = selItems.Count;
                pbCopyCount.Value = 0;
                if (ylib.messageBox(this.Owner, $"{srcFolder} から\n{destFolder} に\n{selItems.Count} ファイル コピーします",
                    "", "確認", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                    foreach (DiffFile fileData in selItems) {
                        string srcPath = fileData.getPath(srcFolder);
                        string destPath = fileData.getPath(destFolder);
                        System.Diagnostics.Debug.WriteLine($"{srcPath} {destPath}");
                        ylib.fileCopy(srcPath, destPath, copyType); pbCopyCount.Value++;
                    }
                }
            }
        }

        /// <summary>
        /// フォルダ比較表示
        /// </summary>
        /// <param name="srcFolder">比較元</param>
        /// <param name="dstFolder">比較先</param>
        /// <param name="hash">ハッシュで比較</param>
        /// <param name="targetFile">比較ファイル</param>
        /// <param name="exceptFile">除外ファイル</param>
        /// <param name="exceptFolder">除外フォルダ</param>
        private void setDiffFolder(string srcFolder, string dstFolder, bool hash, string targetFile, string exceptFile, string exceptFolder)
        {
            mDiffFolder = new DirectoryDiff(srcFolder, dstFolder, hash, targetFile, exceptFile, exceptFolder);
            dispDiffFolder();
        }

        /// <summary>
        /// 比較リスト表示
        /// </summary>
        private void dispDiffFolder()
        {
            List<FilesData> files = mDiffFolder.stripSameFile(rbDiffFile.IsChecked == true);        //  再ファイルのみ表示
            if (mDiffFileList == null)
                mDiffFileList = new List<DiffFile>();
            mDiffFileList.Clear();
            foreach (FilesData filesData in files) {
                mDiffFileList.Add(new DiffFile(filesData));
            }
            dgDiffFolder.ItemsSource = new ReadOnlyCollection<DiffFile>(mDiffFileList);
        }

        /// <summary>
        /// 保存された設定値を設定する
        /// </summary>
        private void setFoldeTitleList()
        {
            string[] buf = new string[6];
            buf[0] = cbFolderTitle.Text;
            buf[1] = tbSrcFolder.Text;
            buf[2] = tbDstFolder.Text;
            buf[3] = tbTargetFile.Text;
            buf[4] = tbExceptFile.Text;
            buf[5] = tbExceptFolder.Text;
            if (mFolderTitleList != null) {
                int index = mFolderTitleList.FindIndex(x => x[0] == cbFolderTitle.Text);
                if (0 <= index)
                    mFolderTitleList.RemoveAt(index);
                mFolderTitleList.Insert(0, buf);
            } else {
                mFolderTitleList = new List<string[]> { buf };
            }
            if (mFolderTitleList != null)
                cbFolderTitle.ItemsSource = mFolderTitleList.ConvertAll(x => x[0]);
        }

        /// <summary>
        /// ファイル比較ツールの設定
        /// </summary>
        private void setDiffTool()
        {
            InputBox dlg = new InputBox();
            dlg.mFileSelectMenu = true;
            dlg.mMainWindow = this;
            dlg.Title = "ファイル比較ツール";
            dlg.mEditText = mDiffTool;
            var result = dlg.ShowDialog();
            if (result == true) {
                mDiffTool = dlg.mEditText;
            }
        }

        /// <summary>
        /// ファイルリストを保存する
        /// </summary>
        /// <param name="path">パス</param>
        private void saveTitleList(string path)
        {
            ylib.saveCsvData(path, mTitleListFormat.ToArray(), mFolderTitleList);
        }

        /// <summary>
        /// ファイルリストを読み込む
        /// </summary>
        /// <param name="path">パス</param>
        private void loadTitleList(string path)
        {
            mFolderTitleList = ylib.loadCsvData(path, mTitleListFormat.ToArray());
            if (mFolderTitleList != null && mMaxTitleListCount < mFolderTitleList.Count)
                mFolderTitleList.RemoveRange(mMaxTitleListCount, mFolderTitleList.Count - mMaxTitleListCount);
        }
    }
}
