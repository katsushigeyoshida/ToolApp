using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WpfLib;

namespace ToolApp
{
    public class DiffFile
    {
        public string mFileName { get; set; }
        public string mRelPath { get; set; }
        public DateTime mSrcLastDate { get; set; }
        public long mSrcSize { get; set; }
        public DateTime mDstLastDate { get; set; }
        public long mDstSize { get; set; }

        public DiffFile(string mFileName, string mRelPath, DateTime mSrcLastDate, int mSrcSize, DateTime mDstLastDate, int mDstSize)
        {
            this.mFileName = mFileName;
            this.mRelPath = mRelPath;
            this.mSrcLastDate = mSrcLastDate;
            this.mSrcSize = mSrcSize;
            this.mDstLastDate = mDstLastDate;
            this.mDstSize = mDstSize;
        }

        public DiffFile(FilesData fileData)
        {
            mFileName = Path.GetFileName(fileData.mRelPath);
            mRelPath = Path.GetDirectoryName(fileData.mRelPath);
            mSrcLastDate = fileData.mSrcFile == null ? new DateTime() : fileData.mSrcFile.LastWriteTime;
            mSrcSize = fileData.mSrcFile == null ? 0 : fileData.mSrcFile.Length;
            mDstLastDate = fileData.mDstFile == null ? new DateTime() : fileData.mDstFile.LastWriteTime;
            mDstSize = fileData.mDstFile == null ? 0 : fileData.mDstFile.Length;
        }
    }

    /// <summary>
    /// DiffFolder.xaml の相互作用ロジック
    /// </summary>
    public partial class DiffFolder : Window
    {
        private string mSrcFolder = @"D:\DATA\Document\KNote";
        private string mDstFolder = @"C:\Users\k-yos\OneDrive\ドキュメント\Document\KNote";

        private DirectoryDiff mDiffFolder;
        private List<DiffFile> mDiffFileList;
        private List<string[]> mFolderTitleList = new List<string[]>() {
            new string[] {"KNote",  @"D:\DATA\Document\KNote", @"C:\Users\k-yos\OneDrive\ドキュメント\Document\KNote" },
        };
        private List<string> mTitleListFormat = new List<string>() {
            "Title", "SrcFolder", "DestFolder", "TargetFile", "ExceptFile", "ExceptFolder",
        };
        private string mTitleListPath = "FolderTitleList.csv";

        private YLib ylib = new YLib();


        public DiffFolder()
        {
            InitializeComponent();

            loadTitleList(mTitleListPath);
            if (mFolderTitleList != null) {
                cbFolderTitle.ItemsSource = mFolderTitleList.ConvertAll(x => x[0]);
                //cbSrcFolder.ItemsSource = mFolderTitleList.ConvertAll(x => x[1]);
                //cbDstFolder.ItemsSource = mFolderTitleList.ConvertAll(x => x[2]);
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cbFolderTitle.SelectedIndex = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveTitleList(mTitleListPath);
        }

        private void cbFolderTitle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= cbFolderTitle.SelectedIndex) {
                //cbSrcFolder.SelectedIndex = cbFolderTitle.SelectedIndex;
                //cbDstFolder.SelectedIndex = cbFolderTitle.SelectedIndex;
            }
        }

        private void tbSrcFolder_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string srcFlder = ylib.folderSelect(tbSrcFolder.Text);
            if (srcFlder != null && 0 < srcFlder.Length) {
                tbSrcFolder.Text = srcFlder;
            }
        }

        private void tbDstFolder_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string dstFlder = ylib.folderSelect(tbDstFolder.Text);
            if (dstFlder != null && 0 < dstFlder.Length) {
                tbDstFolder.Text = dstFlder;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button bt = (Button)e.Source;
            if (bt.Name.CompareTo("btComper") == 0) {
                if (Directory.Exists(tbSrcFolder.Text) || Directory.Exists(tbDstFolder.Text)) {
                    setDiffFolder(tbSrcFolder.Text, tbDstFolder.Text);
                    setFoldeTitleList();
                } else {
                    MessageBox.Show("フォルダが存在しません");
                }
            } else if (bt.Name.CompareTo("btRightUpdate") == 0) {
                selectCopy();
            } else if (bt.Name.CompareTo("btLeftUpdate") == 0) {
            }
        }

        private void selectCopy()
        {
            IList selitems = dgDiffFolder.SelectedItems;
            if (0 < selitems.Count) {
                foreach (DiffFile filesData in selitems) {
                    System.Diagnostics.Debug.WriteLine($"{filesData.mRelPath}\\{filesData.mFileName}");

                }
            }
        }

        private void setDiffFolder(string srcFolder, string dstFolder)
        {
            mSrcFolder = srcFolder;
            mDstFolder = dstFolder;
            mDiffFolder = new DirectoryDiff(srcFolder, dstFolder);
            mDiffFolder.stripSameFile();
            if (mDiffFileList == null)
                mDiffFileList = new List<DiffFile>();
            mDiffFileList.Clear();
            foreach (FilesData filesData in mDiffFolder.mFiles) {
                mDiffFileList.Add(new DiffFile(filesData));
            }
            dgDiffFolder.ItemsSource = new ReadOnlyCollection<DiffFile>(mDiffFileList);
        }

        private void setFoldeTitleList()
        {
            if (mFolderTitleList != null) {
                int index = mFolderTitleList.FindIndex(x => x[0] == cbFolderTitle.Text);
                if (0 <= index) {
                    mFolderTitleList[index][1] = tbSrcFolder.Text;
                    mFolderTitleList[index][2] = tbDstFolder.Text;
                    mFolderTitleList[index][3] = tbTargetFile.Text;
                    mFolderTitleList[index][4] = tbExceptFile.Text;
                    mFolderTitleList[index][5] = tbExceptFolder.Text;
                } else {
                    string[] buf = new string[6];
                    buf[0] = cbFolderTitle.Text;
                    buf[1] = tbSrcFolder.Text;
                    buf[2] = tbDstFolder.Text;
                    buf[3] = tbTargetFile.Text;
                    buf[4] = tbExceptFile.Text;
                    buf[5] = tbExceptFolder.Text;
                    mFolderTitleList.Add(buf);
                }
            } else {
                mFolderTitleList = new List<string[]>();
                string[] buf = new string[6];
                buf[0] = cbFolderTitle.Text;
                buf[1] = tbSrcFolder.Text;
                buf[2] = tbDstFolder.Text;
                buf[3] = tbTargetFile.Text;
                buf[4] = tbExceptFile.Text;
                buf[5] = tbExceptFolder.Text;
                mFolderTitleList.Add(buf);
            }
        }

        private void saveTitleList(string path)
        {
            ylib.saveCsvData(path, mTitleListFormat.ToArray(), mFolderTitleList);
        }

        private void loadTitleList(string path)
        {
            mFolderTitleList = ylib.loadCsvData(path, mTitleListFormat.ToArray());
        }
    }
}
