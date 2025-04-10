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

namespace NurseStation
{
    // SerialPortViewModel.cs
    

    public class SerialPortViewModel 
    {
        #region 单例模式
        private static SerialPortViewModel _instense;
        private static object _lock = new object();
        public static SerialPortViewModel Instense
        {
            get
            {
                return _instense;
            }
            set
            {
                lock (_lock)
                {
                    if (_instense == null)
                    {
                        _instense = new SerialPortViewModel();
                    }
                }
            }
        }

        #endregion
        
        private static string portName =  "COM1";
        private static int baudRate = 9600;
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
        public  void SendData(string data)
        {
            using (var serialPort = new SerialPort(portName, baudRate))
            {
                try
                {
                    serialPort.Open();
                    serialPort.WriteLine(data);
                }
                catch (Exception ex)
                {
                    // 根据需要处理异常
                    throw new IOException($"发送失败: {ex.Message}", ex);
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
