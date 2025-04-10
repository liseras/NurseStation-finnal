using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WardCallSystemNurseStation;

namespace WardCallSystemNurseStation
{
    class CallRecordRepository
    {
        #region 单例模式
        private static CallRecordRepository _instance;
        private static object _Slock = new object();    
        public static CallRecordRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_Slock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CallRecordRepository(@"Server=(localdb)\MSSQLLocalDB;Database=MySqlBace;Integrated Security=True;MultipleActiveResultSets=True;");
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion
        private readonly string _connectionString;

        public CallRecordRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void InsertCallRecord(CallRecord record)
        {
            var query = @"
            INSERT INTO CallRecords 
            (CallTime, WardNumber, PatientName, NurseName, CallStatus) 
            VALUES 
            (@CallTime, @WardNumber, @PatientName, @NurseName, @CallStatus)";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@CallTime", record.CallTime);
                cmd.Parameters.AddWithValue("@WardNumber", record.WardNumber);
                cmd.Parameters.AddWithValue("@PatientName", record.PatientName);
                cmd.Parameters.AddWithValue("@NurseName", record.NurseName);
                cmd.Parameters.AddWithValue("@CallStatus", record.Status);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public ObservableCollection<CallRecord> GetByTimeRange(DateTime startTime, DateTime endTime)
        {
            var query = @"
            SELECT * FROM CallRecords 
            WHERE CallTime BETWEEN @StartTime AND @EndTime";

            return ExecuteQuery(query,
                new SqlParameter("@StartTime", startTime),
                new SqlParameter("@EndTime", endTime));
        }

        // 根据病房号查询
        public ObservableCollection<CallRecord> GetByWardNumber(int wardNumber)
        {
            var query = "SELECT * FROM CallRecords WHERE WardNumber = @WardNumber";
            return ExecuteQuery(query, new SqlParameter("@WardNumber", wardNumber));
        }

        // 根据患者姓名查询（精确匹配）
        public ObservableCollection<CallRecord> GetByPatientName(string patientName)
        {
            var query = "SELECT * FROM CallRecords WHERE PatientName = @PatientName";
            return ExecuteQuery(query, new SqlParameter("@PatientName", patientName));
        }

        // 根据护士姓名查询（精确匹配）
        public ObservableCollection<CallRecord> GetByNurseName(string nurseName)
        {
            var query = "SELECT * FROM CallRecords WHERE NurseName = @NurseName";
            return ExecuteQuery(query, new SqlParameter("@NurseName", nurseName));
        }

        // 根据状态查询
        public ObservableCollection<CallRecord> GetByCallStatus(string CallStatus)
        {
            var query = "SELECT * FROM CallRecords WHERE CallStatus = @CallStatus";
            return ExecuteQuery(query, new SqlParameter("@CallStatus", CallStatus));
        }

        // 组合查询（示例）
        public ObservableCollection<CallRecord> GetFilteredRecords(
            DateTime? startTime = null,
            DateTime? endTime = null,
            int? wardNumber = null,
            string patientName = null,
            string nurseName = null,
            string CallStatus = null)
        {
            var query = new StringBuilder("SELECT * FROM CallRecords WHERE 1=1");

            var parameters = new ObservableCollection<SqlParameter>();

            if (startTime.HasValue && endTime.HasValue)
            {
                query.AppendLine(" AND CallTime BETWEEN @StartTime AND @EndTime");
                parameters.Add(new SqlParameter("@StartTime", startTime.Value));
                parameters.Add(new SqlParameter("@EndTime", endTime.Value));
            }

            if (wardNumber.HasValue)
            {
                query.AppendLine(" AND WardNumber = @WardNumber");
                parameters.Add(new SqlParameter("@WardNumber", wardNumber.Value));
            }

            if (!string.IsNullOrEmpty(patientName))
            {
                query.AppendLine(" AND PatientName = @PatientName");
                parameters.Add(new SqlParameter("@PatientName", patientName));
            }

            if (!string.IsNullOrEmpty(nurseName))
            {
                query.AppendLine(" AND NurseName = @NurseName");
                parameters.Add(new SqlParameter("@NurseName", nurseName));
            }

            if (!string.IsNullOrEmpty(CallStatus))
            {
                query.AppendLine(" AND CallStatus = @CallStatus");
                parameters.Add(new SqlParameter("@CallStatus", CallStatus));
            }

            return ExecuteQuery(query.ToString(), parameters.ToArray());
        }

        // 通用查询执行方法
        public ObservableCollection<CallRecord> ExecuteQuery(string query, params SqlParameter[] parameters)
        {
            var results = new ObservableCollection<CallRecord>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddRange(parameters);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new CallRecord
                        (
                            Convert.ToDateTime(reader["CallTime"]),
                            Convert.ToString(reader["WardNumber"]),
                            reader["PatientName"].ToString(),
                            reader["NurseName"].ToString(),
                            reader["CallStatus"].ToString()
                        ));
                    }
                }
            }

            return results;
        }
        public void GetAllCallRecords(ObservableCollection<CallRecord> results)
        {
            

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT * FROM CallRecords", conn)) // 固定查询语句
                {
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // 确保字段名与数据库列名完全匹配
                            results.Add(new CallRecord(
                                Convert.ToDateTime(reader["CallTime"]),  // DATETIME 类型
                                reader["WardNumber"].ToString(),        // NVARCHAR 类型
                                reader["PatientName"].ToString(),       // NVARCHAR 类型
                                reader["NurseName"]?.ToString() ?? "未分配", // 处理可能的 NULL 值
                                reader["CallStatus"].ToString()             // NVARCHAR 类型
                            ));
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // 捕获数据库特定错误
                Loger.Instence.SaveLog($"数据库查询失败: {ex.Message}");
                MessageBox.Show($"数据库错误: {ex.Number} - {ex.Message}");
            }
            catch (Exception ex)
            {
                Loger.Instence.SaveLog($"系统错误: {ex.Message}");
            }

           
        }
    }
}


    // 根据时间范围查询
   

