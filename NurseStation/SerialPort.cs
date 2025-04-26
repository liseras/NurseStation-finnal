using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using WardCallSystemNurseStation;
using System.Windows;

namespace WardCallSystemNurseStation
{
    // SerialPortViewModel.cs
    

    public class SerialPortViewModel 
    {
        #region 单例模式
        private static SerialPortViewModel _instance;

        // 静态锁对象，用于线程同步
        private static readonly object _lock = new object();

        // 私有构造函数，防止外部实例化
        private SerialPortViewModel()
        {
            // 初始化逻辑（如果需要）
        }

        // 公共静态属性，提供线程安全的单例访问
        public static SerialPortViewModel Instance
        {
            get
            {
                if (_instance == null) // 双重检查锁定
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SerialPortViewModel();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        private static string portName = "COM3";
        private static int baudRate = 115200;
        public static void SendData(string portName, int baudRate, string data)
        {
            using (var serialPort = new SerialPort(portName, baudRate))  //Parity.None,8,StopBits.One,Handshake.None,SerialPort.InfiniteTimeout
            {
                try
                {
                    serialPort.Open();
                    serialPort.WriteLine(data);
                }
                catch (Exception ex)
                {
                    // 根据需要处理异常
                    Loger.Instence.SaveLog($"发送失败: {ex.Message}");
                }
            }
        }
        private static readonly object Lock = new object();
        public void SendData(string data)
        {
            lock (Lock)
            {
                using (var serialPort = new SerialPort(portName, baudRate))
                {

                    try
                    {
                        serialPort.Open();
                        serialPort.WriteLine(data);
                        serialPort.ReadTimeout = 1000;
                        string res = serialPort.ReadLine();
                        Application.Current.Dispatcher
                            .Invoke(() =>
                            {
                                TcpServer.Instance.DataRecviced.Invoke(res);
                            });
                        serialPort.Close();
                    }
                    catch (TimeoutException)
                    {
                        // 处理超时异常
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            TcpServer.Instance.DataRecviced.Invoke("串口读取超时");
                        });
                    }
                    catch (Exception ex)
                    {
                        // 根据需要处理异常
                        Application.Current.Dispatcher
                             .Invoke(() =>
                             {
                                 TcpServer.Instance.DataRecviced.Invoke("串口访问失败");
                             });
                    }
                }
            }
        }
        // 异步发送方法
        public static async Task SendDataAsync(string portName, int baudRate, string data)
        {
            await Task.Run(() => SendData(portName, baudRate, data));
        }

        // 自定义编码发送
        public static void SendData(string portName, int baudRate, string data, Encoding encoding)
        {
            using (var serialPort = new SerialPort(portName, baudRate))
            {
                try
                {
                    serialPort.Open();
                    byte[] buffer = encoding.GetBytes(data);
                    serialPort.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    throw new IOException($"发送失败: {ex.Message}", ex);
                }
            }
        }
    }
}
