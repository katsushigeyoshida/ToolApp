using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WpfLib;

namespace ToolApp
{
    /// <summary>
    /// FitConverter.xaml の相互作用ロジック
    /// </summary>
    public partial class FitConverter : Window
    {
        private string mFitFilePath = "";
        private FitReader mFitReader;
        private List<string> mFileList = new List<string>();
        private string mFileListName = "FitFieList.csv";
        private YLib ylib = new YLib();

        public FitConverter()
        {
            InitializeComponent();

            if (File.Exists(mFileListName)) {
                List<string> fileList = ylib.loadListData(mFileListName);
                if (0 < fileList.Count) {
                    cbFitPath.Items.Clear();
                    foreach (string file in fileList)
                        if (File.Exists(file))
                            cbFitPath.Items.Add(file);
                }
            }
        }

        /// <summary>
        /// Fitファイルパスの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFitPath_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string folder = 0 < mFitFilePath.Length ? Path.GetDirectoryName(mFitFilePath) : ".";
            string path = ylib.fileSelect(folder, "fit");
            if (0 < path.Length) {
                if (cbFitPath.Items.Contains(path))
                    cbFitPath.Items.Remove(path);
                cbFitPath.Items.Insert(0, path);
                cbFitPath.SelectedIndex = 0;
            }
        }

        private void cbFitPath_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= cbFitPath.SelectedIndex) {
                string path = cbFitPath.Items[cbFitPath.SelectedIndex].ToString();
                loadFile(path);
            }
        }

        /// <summary>
        /// Fitファイルの読込
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btFitGet_Click(object sender, RoutedEventArgs e)
        {
            if (0 < cbFitPath.Text.Length) {
                loadFile(cbFitPath.Text);
            }
        }

        private void loadFile(string path)
        {
            if (mFileList.Contains(path)) 
                mFileList.Remove(path);
            if (File.Exists(path)) {
                mFitFilePath = path;
                mFileList.Insert(0, mFitFilePath);
                getFit(mFitFilePath);
            }
        }


        /// <summary>
        /// GPXデータ変換ファイル保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btGpxSave_Click(object sender, RoutedEventArgs e)
        {
            if (mFileList.Count > 0) {
                string path = Path.GetDirectoryName(mFitFilePath);
                path += "\\GPS_" + mFitReader.mGpsInfoData.mFirstTime.ToString("yyyyMMddHHmmss") + ".gpx";
                convGpx(mFitReader, path);
                MessageBox.Show("GPX変換終了\n" + path);
            }
        }

        /// <summary>
        /// CSVデータ変換ファイル保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btCsvSave_Click(object sender, RoutedEventArgs e)
        {
            if (mFileList.Count > 0) {
                string path = Path.GetDirectoryName(mFitFilePath);
                path += "\\GPS_" + mFitReader.mGpsInfoData.mFirstTime.ToString("yyyyMMddHHmmss") + ".csv";
                convCsv(mFitReader, path);
                MessageBox.Show("CSV変換終了\n" + path);
            }
        }

        /// <summary>
        /// 終了ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            if (mFileList.Count > 0) {
                ylib.saveListData(mFileListName, mFileList);
            }
            Close();
        }

        /// <summary>
        /// FitのGPSデータをGPXファイルに変換
        /// </summary>
        /// <param name="fitReader">FitReader</param>
        /// <param name="gpxPath">GPXファイルパス</param>
        /// <returns>可否</returns>
        private bool convGpx(FitReader fitReader, string gpxPath)
        {
            GpxWriter gpxWriter = new GpxWriter(fitReader.mListGpsData, gpxPath);
            return gpxWriter.writeDataAll();
        }

        /// <summary>
        /// FitのGPSデータをCSVファイルに変換
        /// </summary>
        /// <param name="fitReader">FitReader</param>
        /// <param name="csvPath">CSVファイルパス</param>
        /// <returns>可否</returns>
        private bool convCsv(FitReader fitReader, string csvPath)
        {
            List<string> buf = new List<string>();
            buf.Add("DateTime,Latitude,Longitude,Elevator");
            for (int i = 0; i < fitReader.mListGpsData.Count; i++)
                buf.Add(fitReader.mListGpsData[i].toCsvString());
            ylib.saveListData(csvPath, buf);
            return true;
        }

        /// <summary>
        /// Fitファイルを読み込んでGPSデータに変換
        /// </summary>
        /// <param name="path">Fitファイルパス</param>
        /// <returns>可否</returns>
        private bool getFit(string path)
        {
            tbGpsInfo.Text = "";
            tbGpsData.Text = "";
            mFitReader = new FitReader(path);
            int count = mFitReader.getDataRecordAll(FitReader.DATATYPE.gpxData);
            if (count == 0)
                return false;
            mFitReader.dataChk();    //  エラーデータチェック

            tbGpsInfo.Text = gpsInfoData(mFitReader);
            tbGpsData.Text = getGpsData(mFitReader);

            return true;
        }

        /// <summary>
        /// FitのGPS情報を文字列に変換
        /// </summary>
        /// <param name="fitReader">FitReader</param>
        /// <returns>文字列</returns>
        private string gpsInfoData(FitReader fitReader)
        {
            double distance = fitReader.mGpsInfoData.mDistance;
            double minElevation = fitReader.mGpsInfoData.mMinElevator;
            double maxElevation = fitReader.mGpsInfoData.mMaxElevator;
            DateTime firstTime = fitReader.mGpsInfoData.mFirstTime;
            DateTime lastTime = fitReader.mGpsInfoData.mLastTime;

            string buffer = "";
            TimeSpan spanTime = lastTime - firstTime;
            buffer += "開始: " + firstTime.ToString("yyyy/MM/dd HH:mm:ss") +
                " 終了: " + lastTime.ToString("yyyy/MM/dd HH:mm:ss") +
                " 経過: " + ((spanTime.TotalMinutes < 60.0 * 24.0) ? spanTime.ToString(@"hh\:mm\:ss") : spanTime.ToString(@"d\d\a\y\ hh\:mm\:ss"));
            buffer += "\n移動距離: " + distance.ToString("#,##0.## km") +
                " 速度: " + (distance / spanTime.TotalHours).ToString("##0.# km/s") +
                " データ数: " + fitReader.mListGpsData.Count;
            buffer += "\n最大標高: " + maxElevation.ToString("#,##0 m") +
                " 最小標高: " + minElevation.ToString("#,##0 m") +
                " 標高差: " + (maxElevation - minElevation).ToString("#,##0 m");
            return buffer;
        }

        /// <summary>
        /// FitのGPSデータを文字列に変換
        /// </summary>
        /// <param name="fitReader">FitReader</param>
        /// <returns>文字列</returns>
        private string getGpsData(FitReader fitReader)
        {
            string buffer = "";
            for (int i = 0; i < fitReader.mListGpsData.Count; i++)
                buffer += i.ToString("D5") + ": " + fitReader.mListGpsData[i].toString() + "\n";
            return buffer;
        }
    }
}
