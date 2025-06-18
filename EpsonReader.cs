using System;
using System.Collections.Generic;
using System.IO;
using WpfLib;

namespace ToolApp
{
    /// <summary>
    /// Epson GPS Watch SFシリーズ GPSデータ読込
    /// </summary>
    public class EpsonReader
    {
        public List<GpsData> mListGpsData;                      //  GPSデータ(時間/座標/標高)[DATATYPE.gpsData]
        public GpsInfoData mGpsInfoData;                        //  gpsデータ情報

        private List<byte[]> mCodeCount = new List<byte[]>() {
                new byte[] { 0x30, 40 },
                new byte[] { 0x31, 24 },
                new byte[] { 0x40,  8 },
                new byte[] { 0x41, 54 },
                new byte[] { 0x50, 16 },
                new byte[] { 0x60,  4 },
                new byte[] { 0x71,  4 },
            };

        public byte[] mEpsonData;                               //  Epson SFシリーズデータ
        public int mPos = 0;                                    //  読込位置

        private DateTime mDateTime;                             //  時間
        private double mLatitude, mLongitude, mAltitude;        //  GPSデータ

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public EpsonReader(string path)
        {
            if (0 < path.Length && File.Exists(path)) {
                mEpsonData = ylib.loadBinData(path);
                if (mEpsonData != null) {
                    getGpsData();
                    mGpsInfoData = getGpsInfoData();
                } else {
                }
            }
        }

        /// <summary>
        /// GPSデータの読込
        /// </summary>
        public void getGpsData()
        {
            byte[] data = mEpsonData;
            int lapCount = data[0x6c];
            int address = 0x80;
            while (data[address] == 0xFF)
                address += 64;
            address += lapCount * 128;
            mDateTime = getGpsHeadTime(data, address);
            if (mListGpsData == null) mListGpsData = new List<GpsData>();
            mLatitude = 0;
            mLongitude = 0;
            int skipCount = 0;
            while (address < data.Length - 32) {
                if (skipCount == 0 && data[address] == 0x30 && (data[address + 1] == 0x10 || data[address + 1] == 0x20)) {
                    GpsData gpsData = getGpsDataAbsolute(data, address);
                    if (gpsData != null)
                        mListGpsData.Add(gpsData);
                    address += 40;
                } else if (skipCount == 0 && data[address] == 0x31 && (data[address + 1] == 0x10 || data[address + 1] == 0x20)) {
                    mListGpsData.Add(getGpsDataRelative(data, address));
                    address += 24;
                }
                skipCount = skipChk(data[address], skipCount, mCodeCount);
                address++;
            }
        }

        /// <summary>
        /// データ種別コードのデータサイズ
        /// </summary>
        /// <param name="code">種別コード</param>
        /// <param name="skipCount">読飛ばしサイズ</param>
        /// <param name="codeCount">種別コートリスト</param>
        /// <returns>読飛ばしサイズ - 1</returns>
        private int skipChk(byte code, int skipCount, List<byte[]> codeCount)
        {
            if (skipCount == 0) {
                for (int i = 0; i < codeCount.Count; i++) {
                    if (codeCount[i][0] == code)
                        return codeCount[i][1] - 1;
                }
            }
            return 0 < skipCount ? skipCount - 1 : 0;
        }


        /// <summary>
        /// 日時の初期値の取得
        /// </summary>
        /// <param name="data">バイトデータ</param>
        /// <param name="pos">位置</param>
        /// <returns>日時</returns>
        private DateTime getGpsHeadTime(byte[] data,int pos)
        {
            DateTime start = new DateTime(2000+data[pos + 2],data[pos + 3],data[pos + 4],
                data[pos + 5],data[pos + 6],data[pos + 7]);
            return start;
        }

        /// <summary>
        /// 絶対座標の取得
        /// </summary>
        /// <param name="data">バイトデータ</param>
        /// <param name="pos">位置</param>
        /// <returns>座標</returns>
        private GpsData getGpsDataAbsolute(byte[] data, int pos)
        {
            double latitude  = BitConverter.ToInt32(data, pos + 0x04) / (double)0x400000;
            double longitude = BitConverter.ToInt32(data, pos + 0x08) / (double)0x400000;
            double altitude  = BitConverter.ToInt32(data, pos + 0x0C) / (double)0x40;
            if (1.0 < latitude && 1.0 < longitude) {
                mLatitude = latitude;
                mLongitude = longitude;
                mAltitude = altitude;
            } else {
                return null;
            }
            GpsData gpsData = new GpsData();
            gpsData.mLongitude = mLongitude;
            gpsData.mLatitude = mLatitude;
            gpsData.mElevator = mAltitude;
            gpsData.mDateTime = mDateTime;
            mDateTime = mDateTime.AddSeconds(1);
            return gpsData;
        }

        /// <summary>
        /// 相対座標の取得
        /// </summary>
        /// <param name="data">バイトデータ</param>
        /// <param name="pos">位置</param>
        /// <returns>座標</returns>
        private GpsData getGpsDataRelative(byte[] data, int pos)
        {
            double latitude = BitConverter.ToInt16(data, pos + 0x02) / (double)0x400000;
            double longitude = BitConverter.ToInt16(data, pos + 0x04) / (double)0x400000;
            double altitude = BitConverter.ToInt16(data, pos + 0x06) / (double)0x40;
            if (1 > latitude && 1.0 > longitude) {
                mLatitude += latitude;
                mLongitude += longitude;
                mAltitude += altitude;
            }
            GpsData gpsData = new GpsData();
            gpsData.mLongitude = mLongitude;
            gpsData.mLatitude = mLatitude;
            gpsData.mElevator = mAltitude;
            gpsData.mDateTime = mDateTime;
            mDateTime = mDateTime.AddSeconds(1);
            return gpsData;
        }

        /// <summary>
        /// GPS情報をGPSデータから取得
        /// </summary>
        /// <returns>GPS情報</returns>
        public GpsInfoData getGpsInfoData()
        {
            if (mListGpsData != null) {
                GpsInfoData gpsInfoData = new GpsInfoData();
                gpsInfoData.setData(mListGpsData);
                return gpsInfoData;
            }
            return null;
        }
    }
}
