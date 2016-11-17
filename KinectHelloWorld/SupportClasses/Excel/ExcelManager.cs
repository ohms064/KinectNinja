using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace KinectHelloWorld.SupportClasses.Excel {
    class ExcelManager {
        public const string TOTAL = "Total";
        public const string PLAYER = "Player";
        public const string IMPRESSIONS = "Impressions";
        public const string MACHINES = "All machines";
        public const string DATE = "Date";
        public const string APPLICATION_START = "Application Start";
        public const string LOSES = "Losses";
        public const string WINS = "Wins";
        public const string DATA = "Data";
        public const string PRODUCTS_GIVEN = "Products Given Away";
        public const string MALE = "Male";
        public const string FEMALE = "Female";
        public const string UNKNOWN = "Unknown";

        public static ExcelManager instance;
        Dictionary<string, int> columns;
        SortedDictionary<string, ExcelRow> content;
        protected int lastRow = 0;
        protected bool isNewFile = false;
        ExcelWorksheet ws;
        ExcelPackage pck;

        public static ExcelManager CreateSingleton(string outputDir, string outputFileName) {
            instance = new ExcelManager(outputDir, outputFileName);
            return instance;
        }

        public static ExcelManager CreateSingleton(string outputFileName) {
            instance = new ExcelManager(outputFileName);
            return instance;
        }

        public ExcelManager(string outputDir, string outputFileName) {
            InitColumnsDict();
            if( !outputFileName.EndsWith(".xlsx") ) {
                outputFileName += ".xlsx";
            }
            string fileDir = string.Format("{0}\\{1}", outputDir, outputFileName);
            isNewFile = !File.Exists(fileDir);
            FileInfo newFile = new FileInfo(fileDir);
            pck = new ExcelPackage(newFile);
            if( isNewFile) {
                ws = pck.Workbook.Worksheets.Add(DATA);
                CreateTemplate(ref ws);
                content = new SortedDictionary<string, ExcelRow>();
            }
            else {
                ws = pck.Workbook.Worksheets[DATA];
                InitContentDict();
            }
        }

        public ExcelManager(string outputFileName) {
            InitColumnsDict();
            if( !outputFileName.EndsWith(".xlsx") ) {
                outputFileName += ".xlsx";
            }
            isNewFile = !File.Exists(outputFileName);
            FileInfo newFile = new FileInfo(outputFileName);
            pck = new ExcelPackage(newFile);
            if( isNewFile ) {
                ws = pck.Workbook.Worksheets.Add(DATA);
                CreateTemplate(ref ws);
                content = new SortedDictionary<string, ExcelRow>();
            }
            else {
                ws = pck.Workbook.Worksheets[DATA];
                InitContentDict();
            }
        }

        public void WriteContentToFile() {
            int currentRow = 4;
            foreach(KeyValuePair<string, ExcelRow> value in content ) {
                ws.Cells[currentRow, 2].Value = value.Key;
                for( int col = 3; col <= 12; col++ ) {
                    ws.Cells[currentRow, col].Value = value.Value.GetData(col);
                }
                currentRow++;
            }
        }

        public void Save() {
            pck.Save();
        }

        public void Close() {
            pck.Dispose();
        }

        public void AddOrUpdate(string date, ExcelRow newRowData) {
            if( content.ContainsKey(date) ) {
                ExcelRow oldRowData = content[date] as ExcelRow;
                newRowData.AddOther(oldRowData);
                content[date] = newRowData;
            }
            else {
                newRowData.appStart = DateTime.Now.GetTime();
                content.Add(date, newRowData);
            }
            WriteContentToFile();
            Save();
        }

        private void SetHeader(ref ExcelRange range, string value) {
            range.Merge = true;
            range.Value = value;
        }

        private void CreateTemplate(ref ExcelWorksheet ws) {
            ExcelRange machineHeader = ws.Cells["B2:E2"];
            SetHeader(ref machineHeader, MACHINES);

            ExcelRange playerHeader = ws.Cells["F2:H2"];
            SetHeader(ref playerHeader, PLAYER);

            ExcelRange impressionsHeader = ws.Cells["I2:K2"];
            SetHeader(ref impressionsHeader, IMPRESSIONS);

            ws.Cells["B3"].Value = DATE;
            ws.Cells["C3"].Value = APPLICATION_START;
            ws.Cells["D3"].Value = WINS;
            ws.Cells["E3"].Value = LOSES;
            ws.Cells["F3"].Value = PRODUCTS_GIVEN;
              
            ws.Cells["G3"].Value = MALE;
            ws.Cells["H3"].Value = FEMALE;
            ws.Cells["I3"].Value = UNKNOWN;

            ws.Cells["J3"].Value = MALE;
            ws.Cells["K3"].Value = FEMALE;
            ws.Cells["L3"].Value = UNKNOWN;

            ws.Column(2).AutoFit();
        }

        private void InitColumnsDict() {
            columns = new Dictionary<string, int>();
            columns[DATE] = 1;
            columns[APPLICATION_START] = 2;
            columns[WINS] = 3;
            columns[LOSES] = 4;
            columns[PLAYER + MALE] = 5;
            columns[PLAYER + FEMALE] = 6;
            columns[PLAYER + UNKNOWN] = 7;
            columns[IMPRESSIONS + MALE] = 8;
            columns[IMPRESSIONS + FEMALE] = 9;
            columns[IMPRESSIONS + UNKNOWN] = 10;
        }

        private void InitContentDict() {
            int currentRow = 4;
            content = new SortedDictionary<string, ExcelRow>();
            string rowContent = "";
            while( ws.Cells[currentRow, 2].Value != null ) {
                ExcelRow rowData = new ExcelRow();
                for(int i = 3; i <= 12; i++ ) {
                    rowContent = ws.Cells[currentRow, i].Value.ToString();
                    rowData.SetData(i, rowContent);
                }
                content.Add(ws.Cells[currentRow, 2].Value.ToString(), rowData);
                currentRow++;

            }
        }
    }
}
