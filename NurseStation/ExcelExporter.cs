using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WardCallSystemNurseStation;

namespace WardCallSystemNurseStation
{
    public class ExcelExporter
    {
        #region 单例模式
        private static ExcelExporter _instance;
        private static object _lock = new object();
        public static ExcelExporter Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ExcelExporter();
                        }
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        public void ExportToExcel(ObservableCollection<CallRecord> records)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("CallRecords");

                // 设置表头
                worksheet.Cells[1, 1].Value = "呼叫时间";
                worksheet.Cells[1, 2].Value = "病房号";
                worksheet.Cells[1, 3].Value = "病人姓名";
                worksheet.Cells[1, 4].Value = "接听人";
                worksheet.Cells[1, 5].Value = "通话状态";

                // 填充数据
                for (int i = 0; i < records.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cells[row, 1].Value = records[i].CallTime;
                    worksheet.Cells[row, 2].Value = records[i].WardNumber;
                    worksheet.Cells[row, 3].Value = records[i].PatientName;
                    worksheet.Cells[row, 4].Value = records[i].NurseName;
                    worksheet.Cells[row, 5].Value = records[i].Status;
                }

                // 自动调整列宽
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // 保存文件
                FileInfo excelFile = new FileInfo("Record.XLSX");
             
                package.SaveAs(excelFile);
            }
        }
    }
}
