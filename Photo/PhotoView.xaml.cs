using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WpfLib;

namespace ToolApp
{
    public class PhotoData
    {
        public string title { get; set; }
        public string path { get; set; }
        public BitmapImage image { get; set; }
    }

    /// <summary>
    /// PhotoView.xaml の相互作用ロジック
    /// </summary>
    public partial class PhotoView : Window
    {
        private double mWindowWidth;                            //  ウィンドウの高さ
        private double mWindowHeight;                           //  ウィンドウ幅

        public List<PhotoData> Photos;

        private string mGpxFolder = "";
        private string mDataFolder;
        private List<string> mDataFolders = new List<string>();
        private string mDataFolderListPath = "PhotoViewFolders.csv";
        private ImageView mImageView;
        private GpxReader mGpxReader;

        private YLib ylib = new YLib();

        public PhotoView()
        {
            InitializeComponent();

            WindowFormLoad();

            folderListLoad();
            if (0 < mDataFolders.Count)
                CbFolderList.SelectedIndex = 0;
        }

        /// <summary>
        /// アプリ終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            folderListSave();
            WindowFormSave();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.PhotoViewWidth < 100 ||
                Properties.Settings.Default.PhotoViewHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.PhotoViewHeight) {
                Properties.Settings.Default.PhotoViewWidth = mWindowWidth;
                Properties.Settings.Default.PhotoViewHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.PhotoViewTop;
                Left = Properties.Settings.Default.PhotoViewLeft;
                Width = Properties.Settings.Default.PhotoViewWidth;
                Height = Properties.Settings.Default.PhotoViewHeight;
            }
            mGpxFolder = Properties.Settings.Default.GpxFolder;

        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            Properties.Settings.Default.GpxFolder = mGpxFolder;
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.PhotoViewTop = this.Top;
            Properties.Settings.Default.PhotoViewLeft = this.Left;
            Properties.Settings.Default.PhotoViewWidth = this.Width;
            Properties.Settings.Default.PhotoViewHeight = this.Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// [フォルダリスト]の選択処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbFolderList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= CbFolderList.SelectedIndex) {
                var folder = mDataFolders[CbFolderList.SelectedIndex];
                setPhotoData(folder);
                setFolderList(folder);
            }
        }

        /// <summary>
        /// [フォルダリスト]のマウスダブルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbFolderList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (0 < CbFolderList.Text.Length) {
                mDataFolder = CbFolderList.Text;
                if (Path.GetExtension(mDataFolder).Length <= 0) {
                    mDataFolder = Path.GetDirectoryName(mDataFolder + "\\");
                } else {
                    mDataFolder = Path.GetDirectoryName(mDataFolder);
                }
            }
            //  フォルダ選択
            mDataFolder = ylib.folderSelect(mDataFolder);
            if (0 < mDataFolder.Length) {
                if (setPhotoData(mDataFolder))
                    setFolderList(mDataFolder);
                CbFolderList.Text = mDataFolder;
            }
        }

        /// <summary>
        /// [フォルダリスト]のコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbFolderMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("CbFolderAddMenu") == 0) {
                //  フォルダの選択ダイヤログを開く
                folderSelect();
            } else if (menuItem.Name.CompareTo("CbFolderDelMenu") == 0) {
                //  フォルダを削除する
                folderRemove(CbFolderList.Text);
            } else if (menuItem.Name.CompareTo("CbFolderOpenMenu") == 0) {
                //  フォルダを開く
                ylib.openUrl(CbFolderList.Text);
            } else if (menuItem.Name.CompareTo("CbFolderPasteMenu") == 0) {
                //  クリップボードのフォルダパスを貼り付ける
                string folder = ylib.stripControlCode(Clipboard.GetText());
                if (Directory.Exists(folder)) {
                    if (setPhotoData(folder))
                        setFolderList(folder);
                    CbFolderList.Text = folder;
                }
            }

        }

        /// <summary>
        /// [フォルダリスト]のキー入力(return処理)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbFolderList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) {
                mDataFolder = CbFolderList.Text;
                setFolderList(mDataFolder);
                CbFolderList.SelectedIndex = mDataFolders.IndexOf(mDataFolder);
            }
        }

        /// <summary>
        /// [画像ファイルリスト]選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LvPhotoList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= LvPhotoList.SelectedIndex) {
                //  ファイル選択
                int index = LvPhotoList.SelectedIndex;
                setImageData(Photos[index].path);
            }
        }

        /// <summary>
        /// [画像ファイルリスト]をダブルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LvPhotoList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (0 <= LvPhotoList.SelectedIndex) {
                dispPhotoData(Photos[LvPhotoList.SelectedIndex].path);
            }
        }

        /// <summary>
        /// [画像ファイルリスト]のコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            List<string> fileList = new List<string>();
            if (0 < LvPhotoList.SelectedItems.Count) {
                foreach (var item in LvPhotoList.SelectedItems) {
                    PhotoData photo = (PhotoData)item;
                    fileList.Add(photo.path);
                }
            }
            if (0 <= LvPhotoList.SelectedIndex) {
                //  ファイル選択
                int index = LvPhotoList.SelectedIndex;
                if (menuItem.Name.CompareTo("LvOpenMenu") == 0) {
                    //  開く
                    ylib.fileExecute(Photos[index].path);
                } else if (menuItem.Name.CompareTo("LvDispMenu") == 0) {
                    //  イメージ表示
                    dispPhotoData(Photos[index].path);
                } else if (menuItem.Name.CompareTo("LvCoordinateMenu") == 0) {
                    //  座標編集
                    editCoordinate(Photos[index].path);
                } else if (menuItem.Name.CompareTo("LvGpsCoordinateMenu") == 0) {
                    //  GPS座標追加
                    addGpsCoordinate(fileList);
                } else if (menuItem.Name.CompareTo("LvCommentMenu") == 0) {
                    //  コメント編集
                    editComment(Photos[index].path);
                }
            }
        }

        /// <summary>
        /// [プロパティリスト]のコンテキストメニューコピー処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyCopyMenu_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TbProperty.Text);
        }

        /// <summary>
        /// 画像とExif/IPTC情報の表示
        /// </summary>
        /// <param name="path"></param>
        private void setImageData(string path)
        {
            if (!File.Exists(path))
                return;
            //  画像表示
            BitmapImage bmpImage = ylib.getBitmapImage(path);
            ImPhoto.Source = bmpImage;

            //  ファイルプロパティ表示
            //TbProperty.Text = getFileProperty(Photos[index].path);
            //var propList = GetFilePropertyAll(Photos[index].path);
            //TbProperty.Text = string.Join("\n", propList);
            //  TextBlockに表示
            ExifInfo exifInfo = new ExifInfo(path);
            Point cood = exifInfo.getExifGpsCoordinate();
            TbProperty.Text = "撮影日時: " + exifInfo.getDateTime();
            //List<string> listIptc = ylib.getIPTC(path);
            //TbProperty.Text += "\nIPTC情報: " + (0 < listIptc.Count ? listIptc[listIptc.Count - 1] : "");
            TbProperty.Text += "\n" + ylib.getIPTCall(path);
            TbProperty.Text += "\nコメント: " + exifInfo.getUserComment();
            TbProperty.Text += "\nカメラ情報: " + exifInfo.getCamera();
            TbProperty.Text += "\n撮影条件: " + exifInfo.getCameraSetting();
            TbProperty.Text += "\nGPS座標: " + cood.X.ToString("f6") + "," + cood.Y.ToString("f6");
            TbProperty.Text += "\n画像サイズ: 幅: " + bmpImage.PixelWidth + " 高さ: " + bmpImage.PixelHeight;
            TbProperty.Text += "\n" + exifInfo.getExifInfoAll();
            exifInfo.close();
        }

        /// <summary>
        /// IPTC情報の取得
        /// https://stackoverflow.com/questions/5597079/iptc-net-read-write-c-sharp-library
        /// http://msdn.microsoft.com/en-us/library/system.windows.media.imaging.aspx
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private List<string> getIPTC(string path)
        {
            List<string> iptcList = new List<string>();
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
            var metadata = decoder.Frames[0].Metadata as BitmapMetadata;
            if (metadata != null) {
                //Console.WriteLine(metadata.Keywords.Aggregate((old, val) => old + "; " + val));
                //return metadata.Keywords.Aggregate((old, val) => old + "; " + val);
                iptcList.Add(metadata.CameraManufacturer);
                iptcList.Add(metadata.CameraModel);
                iptcList.Add(metadata.Copyright);
                iptcList.Add(metadata.DateTaken);
                iptcList.Add(metadata.Title);
            }
            return iptcList;
        }

        /// <summary>
        /// ファイルプロパティの取得
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string getFileProperty(string path)
        {
            string info = "";
            info += "名前: " + ylib.GetFilePropertyValue(path, 0);
            info += "\nサイズ: " + ylib.GetFilePropertyValue(path, 1);
            info += "\n撮影日時: " + ylib.GetFilePropertyValue(path, 12);
            info += "\nメーカー: " + ylib.GetFilePropertyValue(path, 32);
            info += "\nモデル: " + ylib.GetFilePropertyValue(path, 30);
            info += "\nExifVer: " + ylib.GetFilePropertyValue(path, 255);
            info += "\n露出時間: " + ylib.GetFilePropertyValue(path, 259);
            info += "\n絞り値: " + ylib.GetFilePropertyValue(path, 260);
            info += "\n焦点距離: " + ylib.GetFilePropertyValue(path, 262);
            return info;
        }

        /// <summary>
        /// ListViewに画像ファイルを設定更新
        /// </summary>
        /// <param name="folder"></param>
        private bool setPhotoData(string folder)
        {
            if (Photos == null) {
                Photos = new List<PhotoData>();
            }
            Photos.Clear();
            string[] files = ylib.getFiles(Path.Combine(folder, "*.jpg"));
            if (files == null || files.Length == 0)
                return false;
            PbLoadPhoto.Minimum = 0;
            PbLoadPhoto.Maximum = files.Length;
            PbLoadPhoto.Value = 0;
            foreach (string file in files) {
                PhotoData photo = new PhotoData();
                //photo.image = ylib.getBitmapImage(file, 100);
                photo.image = ylib.getThumbnailImage(file, 100, 80);
                photo.title = Path.GetFileName(file);
                photo.path = file;
                Photos.Add(photo);
                PbLoadPhoto.Value++;
                ylib.DoEvents();
            }
            LvPhotoList.ItemsSource = new ReadOnlyCollection<PhotoData>(Photos);
            PbLoadPhoto.Value = 0;
            return true;
        }


        /// <summary>
        /// フォルダ名の登録
        /// </summary>
        /// <param name="folder"></param>
        private void setFolderList(string folder)
        {
            if (!Directory.Exists(folder))
                return;
            if (mDataFolders.Contains(folder)) {
                mDataFolders.Remove(folder);
            }
            mDataFolders.Insert(0, folder);
            CbFolderList.ItemsSource = new ReadOnlyCollection<string>(mDataFolders);
        }

        /// <summary>
        /// ファイルからフォルダ名リストを取得
        /// </summary>
        private void folderListLoad()
        {
            if (File.Exists(mDataFolderListPath)) {
                List<string> dataFolders = ylib.loadListData(mDataFolderListPath);
                mDataFolders.Clear();
                foreach (string folder in dataFolders) {
                    string[] files = ylib.getFiles(Path.Combine(folder, "*.jpg"));
                    if (files != null && 0 < files.Length)
                        mDataFolders.Add(folder);
                    if (100 < mDataFolders.Count)
                        break;
                }
                CbFolderList.ItemsSource = new ReadOnlyCollection<string>(mDataFolders);
            }
        }

        /// <summary>
        /// フォルダ名リストをファイルに保存
        /// </summary>
        private void folderListSave()
        {
            ylib.saveListData(mDataFolderListPath, mDataFolders);
        }

        /// <summary>
        /// 座標データの追加・編集
        /// </summary>
        /// <param name="path"></param>
        private void editCoordinate(string path)
        {
            ExifInfo exifInfo = new ExifInfo(path);
            Point coord = exifInfo.getExifGpsCoordinate();
            InputBox dlg = new InputBox();
            dlg.mEditText = coord.Y + "," + coord.X;
            if (dlg.ShowDialog() == true) {
                string[] data = dlg.mEditText.Split(',');
                if (1 <= data.Length) {
                    coord.X = ylib.string2double(data[1]);
                    coord.Y = ylib.string2double(data[0]);
                    if (exifInfo.setExifGpsCoordinate(coord))
                        exifInfo.save();
                }
            }
        }

        /// <summary>
        /// コメントデータの追加・編集
        /// </summary>
        /// <param name="path"></param>
        private void editComment(string path)
        {
            ExifInfo exifInfo = new ExifInfo(path);
            string comment = exifInfo.getUserComment();
            if (comment.Length <= 0)
                comment += ylib.getIPTC(path)[4];
            InputBox dlg = new InputBox();
            dlg.mEditText = comment;
            if (dlg.ShowDialog() == true) {
                if (exifInfo.setUserComment(dlg.mEditText))
                    exifInfo.save();
            }
        }

        /// <summary>
        /// GPXファイルから座標を設定
        /// </summary>
        /// <param name="fileList">ファイルパスリスト</param>
        private void addGpsCoordinate(List<string> fileList)
        {
            string gpxPath = ylib.fileSelect(mGpxFolder, "gpx");
            if (0 < gpxPath.Length) {
                mGpxFolder = Path.GetDirectoryName(gpxPath);
                loadGpxData(gpxPath);
                int count = 0;
                foreach (string path in fileList) {
                    ExifInfo exifInfo = new ExifInfo(path);
                    string datetime = exifInfo.getDateTime();
                    char[] sp = new char[] { ':', ' ' };
                    string[] ta = datetime.Split(sp);
                    datetime = string.Format("{0}/{1}/{2} {3}:{4}:{5}", ta[0], ta[1], ta[2], ta[3], ta[4], ta[5]);
                    DateTime dt = DateTime.Parse(datetime);
                    Point pos = mGpxReader.getCoordinate(dt);
                    if (!pos.isEmpty()) {
                        if (exifInfo.setExifGpsCoordinate(pos)) {
                            exifInfo.save();
                            count++;
                        }
                    }
                }
                MessageBox.Show($"{count}/{fileList.Count}の座標を設定");
            }
        }

        /// <summary>
        /// GPXファイルを読み込む
        /// </summary>
        /// <param name="path"></param>
        private void loadGpxData(string path)
        {
            mGpxReader = new GpxReader(path, GpxReader.DATATYPE.gpxData);
            if (mGpxReader.mListGpsData.Count == 0)
                return;
            mGpxReader.dataChk();                                    //  エラーデータチェック
        }

        /// <summary>
        /// イメージファイルをダイヤログ表示
        /// </summary>
        /// <param name="path"></param>
        private void dispPhotoData(string path)
        {
            if (mImageView != null) {
                mImageView.Close();
            }
            mImageView = new ImageView();
            mImageView.mImageList = Photos.ConvertAll(p => p.path);
            mImageView.mImagePath = path;
            mImageView.Show();
        }

        /// <summary>
        /// フォルダリストから削除
        /// </summary>
        /// <param name="folder">削除フォルダ</param>
        private void folderRemove(string folder)
        {
            if (0 < folder.Length) {
                int n = mDataFolders.IndexOf(folder);
                if (0 <= n) {
                    mDataFolders.RemoveAt(n);
                    CbFolderList.ItemsSource = new ReadOnlyCollection<string>(mDataFolders);
                    CbFolderList.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// フォルダの選択追加
        /// </summary>
        private void folderSelect()
        {
            string dataFolder = "";
            if (0 < CbFolderList.Text.Length) {
                dataFolder = CbFolderList.Text;
                if (Path.GetExtension(dataFolder).Length <= 0) {
                    dataFolder = Path.GetDirectoryName(dataFolder + "\\");
                } else {
                    dataFolder = Path.GetDirectoryName(dataFolder);
                }
            }
            //  フォルダ選択
            dataFolder = ylib.folderSelect(dataFolder);
            if (0 < dataFolder.Length) {
                if (setPhotoData(dataFolder))
                    setFolderList(dataFolder);
                CbFolderList.Text = dataFolder;
            }
        }
    }
}
