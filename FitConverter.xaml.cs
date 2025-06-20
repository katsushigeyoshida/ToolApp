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
    /// FitConverter.xaml の相互作用ロジック
    /// </summary>
    public partial class FitConverter : Window
    {
        private string mGpsFilePath = "";
        private FitReader mFitReader;
        private EpsonReader mEpsonReader;
        private List<GpsData> mGpsDataList;
        private GpsInfoData mGpsInfoData;

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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (cbFitPath.Items.Count > 0) {
                List<string> list = new List<string>();
                foreach (string file in cbFitPath.Items)
                    list.Add(file);
                ylib.saveListData(mFileListName, list);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Fitファイルパスの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbFitPath_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string folder = 0 < mGpsFilePath.Length ? Path.GetDirectoryName(mGpsFilePath) : ".";
            string path = ylib.fileSelect(folder, "fit,bin");
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

        /// <summary>
        /// GPSデータファイルの読込
        /// </summary>
        /// <param name="path"></param>
        private void loadFile(string path)
        {
            if (mFileList.Contains(path)) 
                mFileList.Remove(path);
            if (File.Exists(path)) {
                mGpsFilePath = path;
                mFileList.Insert(0, mGpsFilePath);
                string ext = Path.GetExtension(path).ToLower();
                if (ext == ".fit")
                    getFit(mGpsFilePath);
                else if (ext == ".bin")
                    getEpson(mGpsFilePath);
                else
                    return;
                tbGpsInfo.Text = gpsInfoData(mGpsInfoData);
                tbGpsData.Text = getGpsData(mGpsDataList);
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
                string path = Path.GetDirectoryName(mGpsFilePath);
                path += "\\GPS_" + mGpsInfoData.mFirstTime.ToString("yyyyMMddHHmmss") + ".gpx";
                convGpx(mGpsDataList, path);
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
                string path = Path.GetDirectoryName(mGpsFilePath);
                path += "\\GPS_" + mGpsInfoData.mFirstTime.ToString("yyyyMMddHHmmss") + ".csv";
                convCsv(mGpsDataList, path);
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
            Close();
        }

        /// <summary>
        /// FitのGPSデータをGPXファイルに変換
        /// </summary>
        /// <param name="gpsListdata">FitReader</param>
        /// <param name="gpxPath">GPXファイルパス</param>
        /// <returns>可否</returns>
        private bool convGpx(List<GpsData> gpsListdata, string gpxPath)
        {
            GpxWriter gpxWriter = new GpxWriter(gpsListdata, gpxPath);
            return gpxWriter.writeDataAll(true);
        }

        /// <summary>
        /// FitのGPSデータをCSVファイルに変換
        /// </summary>
        /// <param name="gpsListdata">FitReader</param>
        /// <param name="csvPath">CSVファイルパス</param>
        /// <returns>可否</returns>
        private bool convCsv(List<GpsData> gpsListdata, string csvPath)
        {
            List<string> buf = new List<string>();
            buf.Add("DateTime,Latitude,Longitude,Elevator");
            for (int i = 0; i < gpsListdata.Count; i++)
                buf.Add(gpsListdata[i].toCsvString());
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
            mGpsDataList = mFitReader.mListGpsData;
            mGpsInfoData = mFitReader.mGpsInfoData;

            return true;
        }

        /// <summary>
        /// EpsonWatch SFシリーズのGPSデータの読込
        /// </summary>
        /// <param name="path">ファイル名</param>
        /// <returns></returns>
        private bool getEpson(string path)
        {
            tbGpsInfo.Text = "";
            tbGpsData.Text = "";
            mEpsonReader = new EpsonReader(path);

            mGpsDataList = mEpsonReader.mListGpsData;
            mGpsInfoData = mEpsonReader.mGpsInfoData;

            return true;
        }

        /// <summary>
        /// GPS情報を文字列に変換
        /// </summary>
        /// <param name="fitReader">FitReader</param>
        /// <returns>文字列</returns>
        private string gpsInfoData(GpsInfoData gpsInfoData)
        {
            double distance = gpsInfoData.mDistance;
            double minElevation = gpsInfoData.mMinElevator;
            double maxElevation = gpsInfoData.mMaxElevator;
            DateTime firstTime = gpsInfoData.mFirstTime;
            DateTime lastTime = gpsInfoData.mLastTime;

            string buffer = "";
            TimeSpan spanTime = lastTime - firstTime;
            buffer += "開始: " + firstTime.ToString("yyyy/MM/dd HH:mm:ss") +
                " 終了: " + lastTime.ToString("yyyy/MM/dd HH:mm:ss") +
                " 経過: " + ((spanTime.TotalMinutes < 60.0 * 24.0) ? spanTime.ToString(@"hh\:mm\:ss") : spanTime.ToString(@"d\d\a\y\ hh\:mm\:ss"));
            buffer += "\n移動距離: " + distance.ToString("#,##0.## km") +
                " 速度: " + (distance / spanTime.TotalHours).ToString("##0.# km/s") +
                " データ数: " + mGpsDataList.Count;
            buffer += "\n最大標高: " + maxElevation.ToString("#,##0 m") +
                " 最小標高: " + minElevation.ToString("#,##0 m") +
                " 標高差: " + (maxElevation - minElevation).ToString("#,##0 m");
            return buffer;
        }

        /// <summary>
        /// GPSデータを文字列に変換
        /// </summary>
        /// <param name="fitReader">FitReader</param>
        /// <returns>文字列</returns>
        private string getGpsData(List<GpsData> listGpsData)
        {
            StringBuilder buffer = new StringBuilder();
            for (int i = 0; i < listGpsData.Count; i++)
                buffer.Append(i.ToString("D5") + ": " + listGpsData[i].toString() + "\n");
            return buffer.ToString();
        }
    }
}
