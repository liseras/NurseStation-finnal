using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using OfficeOpenXml;
using System.Windows.Input;
using System.Windows.Controls;
using System.Runtime.CompilerServices;
using DocumentFormat.OpenXml;
using NurseStation;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Diagnostics;
namespace WardCallSystemNurseStation
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _newWardNumber;
        private string _newPatientGender;
        private string _newPatientName;
        private int _newPatientAge;
        private string _newPatientCondition;
        ObservableCollection<CallRecord> FilteredRecords = new ObservableCollection<CallRecord>();

        public string NewWardNumber
        {
            get => _newWardNumber;
            set
            {
                _newWardNumber = value;
                OnPropertyChanged(nameof(NewWardNumber));
            }
        }

        public string NewPatientGender
        {
            get => _newPatientGender;
            set
            {
                _newPatientGender = value;
                OnPropertyChanged(nameof(NewPatientGender));
            }
        }

        public string NewPatientName
        {
            get => _newPatientName;
            set
            {
                _newPatientName = value;
                OnPropertyChanged(nameof(NewPatientName));
            }
        }

        public int NewPatientAge
        {
            get => _newPatientAge;
            set
            {
                _newPatientAge = value;
                OnPropertyChanged(nameof(NewPatientAge));
            }
        }

        public string NewPatientCondition
        {
            get => _newPatientCondition;
            set
            {
                _newPatientCondition = value;
                OnPropertyChanged(nameof(NewPatientCondition));
            }
        }

        SettingConfig settingConfig;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // 设置当前窗口为数据上下文
            WardInfo.Instance.SqlQuarry();
            InitBinding();
            ReCord.SelectedIndex = 0;
            settingConfig = new SettingConfig();

            TcpServer.Instance.StartAsync();
            txtBlock.Text = $"服务器已开启     IP:{GetIPAddress()}       Port:{TcpServer.Instance._port} \n";
            TcpServer.Instance.DataRecviced += (msg) => UpdateTextBlock(msg);
        }

        /// <summary>
        /// 初始化绑定
        /// </summary>
        void InitBinding()
        {
            WardListView.ItemsSource = WardInfo.Instance.LstWard;
            lstNurse.ItemsSource = TcpServer.Instance.ListNurseClient;
            lstWard.ItemsSource = TcpServer.Instance.ListWardClient;
            CallRecordListView.ItemsSource = CallDispatcher.Instance.lstCallRecord;

            CallRecordRepository.Instance.GetAllCallRecords(CallDispatcher.Instance.lstCallRecord);
        }

        /// <summary>
        /// 添加ward
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(NewWardNumber) ||
                    string.IsNullOrWhiteSpace(NewPatientName) ||
                    NewPatientAge <= 0 ||
                    string.IsNullOrWhiteSpace(NewPatientCondition))
                {
                    MessageBox.Show("请填写完整的信息！");
                    return;
                }
                // 创建新对象
                Ward newWard = new Ward(
                    NewWardNumber,
                    NewPatientGender,
                    NewPatientName,
                    NewPatientAge,
                    NewPatientCondition
                );

                // 添加到全局集合（触发 ObservableCollection 的自动更新）
                WardInfo.Instance.LstWard.Add(newWard);

                // 保存到数据库
                WardInfo.Instance.SqlInsert(newWard);

                // 清空输入框
                NewWardNumber = string.Empty;
                NewPatientGender = "男";// 或默认值
                NewPatientName = string.Empty;
                NewPatientAge = 0;
                NewPatientCondition = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show("添加失败：" + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        /// <summary>
        /// 删除ward
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelButton_Click(object sender, RoutedEventArgs e)
        {
            if (WardListView.SelectedItem == null)
            {
                MessageBox.Show("请选择要删除的ward");
                return;
            }
            Ward ward = WardListView.SelectedItem as Ward;
            WardInfo.Instance.DeletePatient(ward.WardNumber);
            WardInfo.Instance.LstWard.Remove(ward);
        }


        private void UpdateTextBlock(string message)
        {
            // 确保在 UI 线程上执行
            Dispatcher.Invoke(() =>
            {
                txtBlock.Text += message + "\n";
            });
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            //TcpServer.Instance.StartAsync();
            //txtBlock.Text = $"服务器已开启     IP:{GetIPAddress()}       Port:{TcpServer.Instance._port} \n";
            //TcpServer.Instance.DataRecviced += (msg) => UpdateTextBlock(msg);
            Button button = sender as Button;
            button.IsEnabled = false;
        }
        /// <summary>
        /// 获取ipv4地址
        /// </summary>
        /// <returns></returns>
        public string GetIPAddress()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            // 查找无线局域网接口（WLAN）
            NetworkInterface wlanInterface = networkInterfaces
                .FirstOrDefault(nic =>
                    nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);

            if (wlanInterface != null)
            {
                // 获取无线网络接口的 IP 地址
                UnicastIPAddressInformation ipv4Address = wlanInterface
                    .GetIPProperties()
                    .UnicastAddresses
                    .FirstOrDefault(addr =>
                        addr.Address.AddressFamily == AddressFamily.InterNetwork);

                if (ipv4Address != null)
                {
                    return ipv4Address.Address.ToString();
                }
            }
            return "";
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Process currentProcess = Process.GetCurrentProcess();
            currentProcess.Kill();

        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            ExcelExporter.Instance.ExportToExcel(CallDispatcher.Instance.lstCallRecord);
            Loger.Instence.SaveLog("EXCEL导出成功");
            MessageBox.Show("EXCEL导出成功");
        }

        private void ReCord_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterRecords();
        }
        private void FilterRecords()
        {
            try
            {
                var selected = (ComboBoxItem)ReCord.SelectedItem;
                var filter = selected?.Content?.ToString() ?? "全部"; // 默认值设为"全部"或其他合理值

                ObservableCollection<CallRecord> query;
                switch (filter)
                {
                    case "全部":
                        query = CallDispatcher.Instance.lstCallRecord;
                        break;
                    case "未接听":
                        query = new ObservableCollection<CallRecord>(
                            CallDispatcher.Instance.lstCallRecord.Where(r => r.Status == "未接听"));
                        break;
                    case "已接听":
                        query = new ObservableCollection<CallRecord>(
                            CallDispatcher.Instance.lstCallRecord.Where(r => r.Status == "已接听"));
                        break;
                    default:
                        query = CallDispatcher.Instance.lstCallRecord;
                        break;
                }

                // 更新绑定数据
                FilteredRecords.Clear();
                foreach (var record in query)
                {
                    FilteredRecords.Add(record);
                }
                if (CallRecordListView != null)
                    CallRecordListView.ItemsSource = FilteredRecords;
            }
            catch (Exception ex)
            {

            }
        }

        private void settingButton_Click(object sender, RoutedEventArgs e)
        {
            SettingConfig settingConfig = new SettingConfig();

            settingConfig.ShowDialog();


        }
    }
       
    /// <summary>
    /// ward
    /// </summary>
    public class WardClient
    {
        public string WardName { get; set; }
        public string WardIP { get; set; }
        public bool WardStatus { get; set; }
        public string WardCard { get; set; }
        public TcpClient Client { get; private set; }
        public event EventHandler Disconnected;
        private NetworkStream _stream;
        private bool _isDisposed = false;
        public WardClient(string name, string ip, string card, bool status,TcpClient client)
        {
            WardName = name;
            WardIP = ip;
            WardStatus = status;
            WardCard = card;
            Client = client;
            _stream = client.GetStream();
          
        }
       
        protected virtual void OnDisconnected()
        {
            if (!_isDisposed)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
                _isDisposed = true; // 防止重复触发
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Client?.Close();
                _isDisposed = true;
            }
        }
        public int SendData(string data)
        {
            if (Client != null && Client.Connected)
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
                _stream.Write(buffer, 0, buffer.Length);
                return 0;
            }
            return -1;
        }
        public string RecvData()
        {
            if(Client != null && Client.Connected)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string data = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    return data;
                }
                    return null;
            }
            else
            {
                return null;
            }
        }
    }
    /// <summary>
    /// 护士
    /// </summary>
    public class NurseClient : IDisposable
    {
        public string NurseName { get; set; }
        public string NurseIP { get; set; }
        public bool NurseStatus { get; set; }
        public string NurseCard { get; set; }
        public bool IsAvailable { get; set; } = true;
        public DateTime LastResponseTime { get; set; }
        public TcpClient Client { get; private set; }

        public NetworkStream Stream => Client?.GetStream();

        public event EventHandler Disconnected;
        private NetworkStream _stream;
        private bool _isDisposed = false;

        public NurseClient() { }
        public NurseClient(string name, string ip, string card, bool status, TcpClient client)
        {
            NurseName = name;
            NurseIP = ip;
            NurseStatus = status;
            NurseCard = card;
            Client = client;
            _stream = client.GetStream();
        }
       
        protected virtual void OnDisconnected()
        {
            if (!_isDisposed)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
                _isDisposed = true; // 防止重复触发
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Client?.Close();
                _isDisposed = true;
            }
        }
        public int SendData(string data)
        {
            if (Client != null && Client.Connected)
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
                _stream.Write(buffer, 0, buffer.Length);
                return 0;
            }
            return -1;
        }
        public string RecvData()
        {
            if (Client != null && Client.Connected)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string data = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    return data;
                }
                return null;
            }
            else
            {
                return null;
            }
        }
    }
    /// <summary>
    /// 呼叫记录
    /// </summary>
    public class CallRecord
    {
        public DateTime CallTime { get; set; }
        public string WardNumber { get; set; }
        public string PatientName { get; set; }
        public string NurseName { get; set; }
        public string Status { get; set; }
        public CallRecord(DateTime callTime, string wardNumber, string patientName, string nurseName, string status)
        {
            CallTime = callTime;
            WardNumber = wardNumber;
            PatientName = patientName;
            NurseName = nurseName;
            Status = status;
        }
    }


}