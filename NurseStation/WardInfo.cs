using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Windows;
namespace NurseStation
{
    public sealed class WardInfo:INotifyCollectionChanged
    {
        #region 单例模式
        // 私有静态字段（volatile确保多线程可见性）
        private static volatile WardInfo _instance;

        // 专用锁对象（避免使用typeof(WardInfo)作为锁）
        private static readonly object _lock = new object();

        // 私有构造函数（防止外部实例化）
        private WardInfo() { }

        // 公共实例访问器
        public static WardInfo Instance
        {
            get
            {
                // 第一次检查（避免不必要的锁定）
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        // 第二次检查（确保只有一个线程创建实例）
                        if (_instance == null)
                        {
                            _instance = new WardInfo();
                        }
                    }
                }
                return _instance;
            }
        }


        #endregion
        public ObservableCollection<Ward> LstWard = new ObservableCollection<Ward>();

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        // 空密码连接字符串
        string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=MySqlBace;Integrated Security=True;MultipleActiveResultSets=True;";

        /// <summary>
        /// 数据库插入
        /// </summary>
        /// <param name="ward"></param>
        public void SqlInsert(Ward ward)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string sqlCommand = "INSERT INTO Patients (WardNumber, PatientName, PatientAge,PatientGender,PatientCondition) VALUES (@ward, @name, @age, @gender, @condition);";
                    using (SqlCommand command = new SqlCommand(sqlCommand, connection))
                    {
                        command.Parameters.AddWithValue("@ward", ward.WardNumber);
                        command.Parameters.AddWithValue("@name", ward.PatientName);
                        command.Parameters.AddWithValue("@age", ward.PatientAge);
                        command.Parameters.AddWithValue("@gender", ward.PatientGender);
                        command.Parameters.AddWithValue("@condition", ward.PatientCondition);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("数据库操作失败：" + ex.Message);
            }
        }
        public async Task SqlQuarry()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sqlCommand = "SELECT * FROM Patients";
                    using (SqlCommand command = new SqlCommand(sqlCommand, connection))
                    {
                        SqlDataReader reader = await command.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            //// 创建Ward对象并添加到listWard中
                            Ward ward = new Ward(
                               reader.GetString(1),
                                reader.GetString(2),
                                reader.GetString(3),
                                reader.GetInt32(4),
                                reader.GetString(6));
                            /// 使用Dispatcher更新UI
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LstWard.Add(ward);
                            });
                        }
                        
                        
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("异步查询失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="wardNumber"></param>
        public void DeletePatient(string wardNumber)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM dbo.Patients WHERE WardNumber = @ward;";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ward", wardNumber);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("删除失败：" + ex.Message);
            }
        }




    }

    /// <summary>
    /// 病房信息
    /// </summary>
    public class Ward
    {
        public string WardNumber { get; set; }
        public string PatientGender { get; set; }
        public string PatientName { get; set; }
        public int PatientAge { get; set; }
        public string PatientCondition { get; set; }
        public Ward(string wardNumber, string patientGender, string patientName, int patientAge, string patientCondition)
        {
            WardNumber = wardNumber;
            PatientGender = patientGender;
            PatientName = patientName;
            PatientAge = patientAge;
            PatientCondition = patientCondition;
        }
    }

   /// <summary>
   /// 性别
   /// </summary>
    public enum Gender
    {
        Male,
        Female
    }
}
