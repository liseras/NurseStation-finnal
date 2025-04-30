using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MySql.Data.MySqlClient;
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
                            _instance = new CallRecordRepository();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion
        //private string _connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=MySqlBace;Integrated Security=True;MultipleActiveResultSets=True;";
        private string _connectionString = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;MultipleActiveResultSets=True;";

        private string databaseName = "MySqlBace";
        private static string createTableCallRecords = @"
        CREATE TABLE dbo.CallRecords (
            RecordID INT PRIMARY KEY IDENTITY(1,1), -- 主键，自增
            WardNumber NVARCHAR(50) NOT NULL,       -- 病房编号，不允许为空
            PatientName NVARCHAR(100) NOT NULL,     -- 患者姓名，不允许为空
            NurseName NVARCHAR(100) NOT NULL,       -- 护士姓名，不允许为空
            CallStatus NVARCHAR(50) NOT NULL,       -- 呼叫状态，不允许为空
            CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(), -- 创建时间，默认为当前时间，不允许为空
            CallTime DATETIME NOT NULL              -- 呼叫时间，不允许为空
        );
        ";

        private static string createTablePatients = @"
        CREATE TABLE dbo.Patients (
            PatientId INT PRIMARY KEY IDENTITY(1,1), -- 主键，自增
            WardNumber NVARCHAR(50) NOT NULL,        -- 病房编号，不允许为空
            PatientGender NVARCHAR(50) NOT NULL,     -- 患者性别，不允许为空
            PatientName NVARCHAR(100) NOT NULL,      -- 患者姓名，不允许为空
            PatientAge INT NOT NULL,                 -- 患者年龄，不允许为空
            CreatedAt DATETIME NULL DEFAULT GETDATE(), -- 创建时间，默认为当前时间，允许为空
            PatientCondition NVARCHAR(100) NULL      -- 患者病情描述，允许为空
        );
        ";
        private static string createTableUsers = @"
            CREATE TABLE dbo.Users (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Username NVARCHAR(50) NOT NULL UNIQUE,
                Password NVARCHAR(255) NOT NULL
        );
        ";
        /*
         * CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL
);
          */

        public CallRecordRepository()
        {

            
        }

        public void SqlServerInit()
        {
            try
            {
                CheckAndCreateDatabase(_connectionString, databaseName);
                _connectionString += $"Database={databaseName};";
                // 检查并创建表
                CheckAndCreateTable(_connectionString, "dbo.CallRecords", createTableCallRecords);
                CheckAndCreateTable(_connectionString, "dbo.Patients", createTablePatients);
                CheckAndCreateTable(_connectionString, "dbo.Users", createTableUsers);
            }
            catch (Exception ex)
            {
                
            }
        }

        /// <summary>
        /// 检查并创建表
        /// </summary>
        static void CheckAndCreateTable(string connectionString, string tableName, string createTableQuery)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // 查询表是否存在
                string query = $@"
                IF NOT EXISTS (
                    SELECT * 
                    FROM sys.tables 
                    WHERE name = N'{tableName}'
                )
                BEGIN
                    {createTableQuery}
                END
            ";

                using (var command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                
            }
        }

        /// <summary>
        /// 检查并创建数据库
        /// </summary>
        static void CheckAndCreateDatabase(string connectionString, string databaseName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // 查询数据库是否存在
                string query = $@"
                IF NOT EXISTS (
                    SELECT name 
                    FROM sys.databases 
                    WHERE name = N'{databaseName}'
                )
                BEGIN
                    CREATE DATABASE [{databaseName}];
                END
            ";

                using (var command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

              
            }
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
   

