using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;
using Path = System.IO.Path;
using System.Reflection;
using System.IO.Ports;
using System.Runtime.CompilerServices;

namespace WardCallSystemNurseStation
{
    /// <summary>
    /// SettingConfig.xaml 的交互逻辑
    /// </summary>
    public partial class SettingConfig : Window
    {
        public SettingConfig()
        {
            InitializeComponent();
            InitializeData();
            LoadConfig();
        }
        // 定义配置项集合
        private Dictionary<string, object> _configs = new Dictionary<string, object>();

       

        private void InitializeData()
        {
     
            // 初始化配置项
            _configs.Add("串口设置", new SerialSettings());
            _configs.Add("网络配置", new NetworkConfig());
            _configs.Add("系统参数", new SystemParameters());

            // 绑定列表项
            lstConfigItems.ItemsSource = _configs.Keys;
            btnSave_Click(new Button(),new RoutedEventArgs());
        }



        // 列表选择事件
        private void lstConfigItems_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (lstConfigItems.SelectedItem is string key && _configs.ContainsKey(key))
            {
                propertyGrid.SelectedObject = _configs[key];
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Config.Instance.SerialSettings = (SerialSettings)_configs["串口设置"];
                Config.Instance.NetworkConfig1 = (NetworkConfig)_configs["网络配置"];
                Config.Instance.SystemParameters = (SystemParameters)_configs["系统参数"];
                var container = new ConfigContainer
                {
                    UserSettings = (SerialSettings)_configs["串口设置"],
                    NetworkConfig = (NetworkConfig)_configs["网络配置"],
                    SystemParameters = (SystemParameters)_configs["系统参数"]
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    // 关键：使用宽松编码器保留中文字符 [[7]][[8]]
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string savePath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
                File.WriteAllText(savePath, JsonSerializer.Serialize(container, options));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void LoadConfig()
        {
            try
            {
                // 修正文件路径获取方式 [[4]][[10]]
                string appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string filePath = Path.Combine(appDirectory ?? string.Empty, "config.json");

                if (File.Exists(filePath))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };

                    // 使用JsonElement作为中间类型 [[8]]
                    var configDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                        File.ReadAllText(filePath),
                        options);

                    foreach (var key in configDict?.Keys ?? Enumerable.Empty<string>())
                    {
                        switch (key)
                        {
                            case "UserSettings":
                                var userSettings = JsonSerializer.Deserialize<SerialSettings>(configDict[key].GetRawText());
                                _configs["串口设置"] = userSettings;
                                break;
                            case "NetworkConfig":
                                var networkConfig = JsonSerializer.Deserialize<NetworkConfig>(configDict[key].GetRawText());
                                _configs["网络配置"] = networkConfig;
                                break;
                            case "SystemParameters":
                                var systemParams = JsonSerializer.Deserialize<SystemParameters>(configDict[key].GetRawText());
                                _configs["系统参数"] = systemParams;
                                break;
                        }
                    }

                    // 确保UI更新在主线程执行
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        lstConfigItems.Items.Refresh();
                        if (lstConfigItems.SelectedItem != null)
                            propertyGrid.SelectedObject = _configs[lstConfigItems.SelectedItem.ToString()];
                    });
                }
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("配置文件未找到", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"配置格式错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    public class ConfigContainer
    {
        public SerialSettings UserSettings { get; set; }
        public NetworkConfig NetworkConfig { get; set; }
        public SystemParameters SystemParameters { get; set; }
    }
    // 示例类 1：用户设置

    public class SerialSettings : INotifyPropertyChanged
    {
        
        // 波特率枚举 [[3]][[8]]
        public enum BaudRateEnum
        {
            [Description("9600 bps")] Baud9600 = 9600,
            [Description("19200 bps")] Baud19200 = 19200,
            [Description("38400 bps")] Baud38400 = 38400,
            [Description("57600 bps")] Baud57600 = 57600,
            [Description("115200 bps")] Baud115200 = 115200
        }

        // 数据位枚举 [[2]][[4]]
        public enum DataBitsEnum
        {
            [Description("5位数据")] Bits5 = 5,
            [Description("6位数据")] Bits6 = 6,
            [Description("7位数据")] Bits7 = 7,
            [Description("8位数据")] Bits8 = 8
        }

        // 停止位枚举 [[6]][[9]]
        public enum StopBitsEnum
        {
            [Description("1位停止位")] One = 1,
            [Description("1.5位停止位")] OnePointFive = 2,
            [Description("2位停止位")] Two = 3
        }

        // 校验位枚举 [[9]][[10]]
        public enum ParityEnum
        {
            [Description("无校验")] None = 0,
            [Description("奇校验")] Odd = 1,
            [Description("偶校验")] Even = 2,
            [Description("标记校验")] Mark = 3,
            [Description("空格校验")] Space = 4
        }

        public string[] AvailablePorts => SerialPort.GetPortNames();

        private string _portName;
        [Category("串口设置")]
        [Description("选择可用串口")]
        [DisplayName("Port Name")]
        [TypeConverter(typeof(PortNameConverter))]
        public string PortName
        {
            get => _portName;
            set => SetField(ref _portName, value);
        }
        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        // 波特率属性
        private BaudRateEnum _baudRate = BaudRateEnum.Baud9600;
        [Category("串口设置")]
        [Description("选择标准波特率")]
        [DisplayName("Baud Rate")]
        [Browsable(true)]
        public BaudRateEnum BaudRate
        {
            get => _baudRate;
            set
            {
                if (_baudRate != value)
                {
                    _baudRate = value;
                    OnPropertyChanged(nameof(BaudRate));
                }
            }
        }

        // 数据位属性
        private DataBitsEnum _dataBits = DataBitsEnum.Bits8;
        [Category("串口设置")]
        [Description("选择数据位数")]
        [DisplayName("Data Bits")]
        [Browsable(true)]
        public DataBitsEnum DataBits
        {
            get => _dataBits;
            set
            {
                if (_dataBits != value)
                {
                    _dataBits = value;
                    OnPropertyChanged(nameof(DataBits));
                }
            }
        }

        // 停止位属性
        private StopBitsEnum _stopBits = StopBitsEnum.One;
        [Category("串口设置")]
        [Description("选择停止位配置")]
        [DisplayName("Stop Bits")]
        [Browsable(true)]
        public StopBitsEnum StopBits
        {
            get => _stopBits;
            set
            {
                if (_stopBits != value)
                {
                    _stopBits = value;
                    OnPropertyChanged(nameof(StopBits));
                }
            }
        }

        // 校验位属性
        private ParityEnum _parity = ParityEnum.None;
        [Category("串口设置")]
        [Description("选择校验方式")]
        [DisplayName("Parity")]
        [Browsable(true)]
        public ParityEnum Parity
        {
            get => _parity;
            set
            {
                if (_parity != value)
                {
                    _parity = value;
                    OnPropertyChanged(nameof(Parity));
                }
            }
        }


        // INotifyPropertyChanged实现 [[1]]
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // 示例类 2：网络配置
    public class NetworkConfig : INotifyPropertyChanged
    {
        
        private int _port = 8888;
        [Category("网络设置")]
        [Description("端口号")]
        [DisplayName("Port")]
        [Browsable(true)]
        public int Port
        {
            get { return _port; }
            set
            {
                if(_port != value)
                {
                    _port = value;
                    OnPropertyChanged(nameof(Port));
                }
            }
        }
        private bool _isAutoStart = false;
        [Category("网络设置")]
        [Description("是否自动开启服务器")]
        [DisplayName("IsAutoStart")]
        [Browsable(true)]
        public bool IsAutoStart
        {
            get { return _isAutoStart; }
            set
            {
                if (_isAutoStart != value)
                {
                    _isAutoStart = value;
                    OnPropertyChanged(nameof(IsAutoStart));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }

    // 示例类 3：系统参数
    public class SystemParameters : INotifyPropertyChanged
    {
       

        public event PropertyChangedEventHandler PropertyChanged;

       

        protected void OnPropertyChanged(string name) =>
           PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class Config
    {
        private static readonly object _lock = new object(); // 静态锁对象 [[5]][[10]]
        private static Config _instance; // 使用静态字段存储实例 [[7]]

        public static Config Instance
        {
            get
            {
                if (_instance == null) // 第一次检查 [[8]]
                {
                    lock (_lock) // 使用专用锁对象 [[2]][[5]]
                    {
                        if (_instance == null) // 第二次检查（双重锁定）[[8]]
                        {
                            _instance = new Config();
                        }
                    }
                }
                return _instance;
            }
        }
        public SystemParameters SystemParameters = new SystemParameters();
        public SerialSettings SerialSettings = new SerialSettings();
        public NetworkConfig NetworkConfig1 = new NetworkConfig();
    }

    // 自定义类型转换器实现动态下拉列表
    // 动态串口名称转换器
    public class PortNameConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(SerialPort.GetPortNames());
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }
    }
}
