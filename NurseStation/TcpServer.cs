using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using NurseStation;
using MySqlX.XDevAPI;
using ZstdSharp.Unsafe;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WardCallSystemNurseStation
{
    public sealed class TcpServer : INotifyCollectionChanged
    {
        private TcpListener listener;
        public int _port = Config.Instance.NetworkConfig1.Port;
        private bool _isRunning;

        /// <summary>
        /// List of nurse clients.
        /// </summary>

        private ObservableCollection<NurseClient> _listNurseClient = new ObservableCollection<NurseClient>();
        public ObservableCollection<NurseClient> ListNurseClient
        {
            get
            {
                return _listNurseClient;
            }

            set
            {
                if (_listNurseClient != value)
                {
                    _listNurseClient = value;

                }
            }

        }

        /// <summary>
        /// List of ward clients.
        /// </summary>
        private ObservableCollection<WardClient> _listWardClient = new ObservableCollection<WardClient>();
        public ObservableCollection<WardClient> ListWardClient
        {
            get
            {
                return _listWardClient;
            }

            set
            {
                if (_listWardClient != value)
                {
                    _listWardClient = value;

                }
            }
        }

        /// <summary>
        /// Data recviced event handler.
        /// </summary>
        public Action<string> DataRecviced;

        #region 单例模式
        private TcpServer()
        {

        }
        private static volatile TcpServer instance;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public static TcpServer Instance
        {
            get
            {
                lock (typeof(TcpServer))
                {
                    if (instance == null)
                    {
                        instance = new TcpServer();
                    }
                }
                return instance;
            }
        }
        #endregion

        /// <summary>
        /// Starts the TCP server.
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
            => await Task.Run(async () =>
            {
                try
                {
                    listener = new TcpListener(IPAddress.Any, _port);
                    listener.Start();
                    _isRunning = true;
                    await AcceptClients();
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
            );

        /// <summary>
        /// Accepts clients.
        /// </summary>
        /// <returns></returns>
        private async Task AcceptClients()
        {
            while (_isRunning)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    //Thread thread = new Thread(() => HandleClient(client));
                    Task task = Task.Run(() => HandleClient(client));
                }
                catch (ObjectDisposedException)
                {
                    // Server stopped
                    Loger.Instence.SaveLog("Server stopped");
                    break;
                }
                catch (Exception ex)
                {

                    Loger.Instence.SaveLog(ex.ToString());
                }
            }
        }
        /// <summary>
        /// Handles the client.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        bool isRead = true;
        private void HandleClient(TcpClient client)
        {

            object lockObj = new object();
            lock (lockObj)
            {
                try
                {
                    var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    Thread.Sleep(100);
                    try
                    {
                        string message = ReadClient(client);
                        #region  下面方法用ReadClient替代
                        //var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                        //var stream = client.GetStream();
                        //var buffer = new byte[1024];
                        //while (_isRunning)
                        //{
                        //    try
                        //    {
                        //            //using (var stream = client.GetStream())

                        //         var bytesRead = stream.Read(buffer, 0, buffer.Length);

                        //         if (bytesRead == 0)
                        //            {

                        //                Application.Current.Dispatcher.Invoke(() =>
                        //                    {
                        //                        ListNurseClient.Remove(ListNurseClient.FirstOrDefault(x => x.NurseIP == clientIp));
                        //                        ListWardClient.Remove(ListWardClient.FirstOrDefault(x => x.WardIP == clientIp));
                        //                    });
                        //                break;
                        //            }




                        //        var message = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

                        //        if (!message.ToLower().Contains("result"))
                        //        {

                        //            DataRecviced?.Invoke(message);
                        //            Loger.Instence.SaveLog(message);
                        //            JsonDeserialize(message.TrimEnd('\0'), client);
                        //        }
                        #endregion
                        DataRecviced?.Invoke(message);
                        Loger.Instence.SaveLog(message);
                        JsonDeserialize(message, client);

                    }

                    catch (Exception ex)
                    {
                        if (!client.Connected)
                        {
                            DataRecviced?.Invoke("Client disconnected");
                            Loger.Instence.SaveLog("Client disconnected");
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ListNurseClient.Remove(ListNurseClient.FirstOrDefault(x => x.NurseIP == clientIp));
                                ListWardClient.Remove(ListWardClient.FirstOrDefault(x => x.WardIP == clientIp));
                            });
                            client.Close();
                        }
                        Loger.Instence.SaveLog(ex.ToString());
                    }
                }
                catch
                {

                }
            }
        }
        /// <summary>
        /// DataType: connect, Call,disconnect
        /// User: nurse, ward
        /// UserName: nurseName, wardName
        /// UserIP: nurseIP, wardIP
        /// UserCard: nurseCard, wardCard
        /// </summary>
        /// <param name="json"></param>
        public void JsonDeserialize(string json, TcpClient client)
        {
            
            JsonNode jsonObject = JsonNode.Parse(json);
            if (jsonObject != null)
            {
                switch ((string)jsonObject["DataMethod"])
                {
                    case "NurseConnect":
                         NurseConnect(jsonObject, client);
                        break;
                    case "NurseLogin":
                        NurseLogin(jsonObject, client);
                        break;
                    case "WardConnect":
                        WardConnect(jsonObject, client);
                        break;
                    case "WardCall":
                        WardCall(jsonObject, client);
                        break;
                }

            }
        }
        /// <summary>
        /// WardCall
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <param name="client"></param>
        private void WardCall(JsonNode jsonObject, TcpClient client)
        {
            isRead = false;
            var wardNumber = (string)jsonObject["WardNumber"];
            CallDispatcher.Instance.PlaceCall (new CallRequest
            {
                WardNumber = wardNumber,
                CallTime = DateTime.Now,
                PatientName = (string)jsonObject["PatientName"],
                Status = CallStatus.Waiting,
                Priority = 1,
            });

            string cmd = (string)jsonObject["WardNumber"] + "|" + "true";
           // SerialPortViewModel.Instense.SendData (cmd);

            isReturn = false;
            List<string> listsendNurse = new List<string>();
            while (!isReturn)
            {
                CallDispatcher.Instance.isPaused = true;

                //最多等待2秒
                int maxWait = 20000;
                int startWait = 0;
                while (CallDispatcher.Instance.isPaused)
                {
                    Thread.Sleep(100);
                    startWait += 100;
                    if (startWait >= maxWait)
                        break;
                }
                try
                {
                    // 添加ward到json
                    Ward ward = WardInfo.Instance.LstWard.FirstOrDefault(w => w.WardNumber == wardNumber);

                    jsonObject = MergeWardData(jsonObject, wardNumber);
                    string buffer = jsonObject.ToJsonString();
                    byte[] data = Encoding.UTF8.GetBytes(buffer);
                    JsonNode responseJson;

                    if (!CallDispatcher.Instance.p2pDic.TryGetValue(wardNumber, out var nurse))
                    {
                        return;
                    }
                    if(listsendNurse.Contains(nurse.NurseCard) || CallDispatcher.Instance.isBusy)
                    {
                        //CallDispatcher.Instance.HandleNurseResponse(nurse.NurseName, false,false);
                        var Wardclient = ListWardClient.Where(x => x.WardCard == (string)jsonObject["WardNumber"]).FirstOrDefault();
                        Wardclient.SendData("{\"DataMethod\":\"WardCall\",\"IsSuccess\":\"false\"}");
                        return;
                    }
                   
                    nurse.SendData(buffer);
                    //添加到拨打过的列表
                    listsendNurse.Add(nurse.NurseCard);
                    var message = nurse.RecvData();
                 
                    var response = message.TrimEnd('\0');

                    // string response =  ReadClient(client);
                    try
                    {
                        
                        DataRecviced?.Invoke(response);
                        //处理护士响应
                         responseJson = JsonNode.Parse(response.TrimEnd('\0'));
                        bool isRusult = CallDispatcher.Instance.HandleNurseResponse((string)responseJson["NurseName"], ((string)responseJson["IsSuccess"] == "True"));
                        //如果时最后结果
                        isReturn = isRusult;
                        if (isRusult)
                        {
                            var ward1 = ListWardClient.FirstOrDefault(x => x.WardCard == (string)responseJson["WardNumber"]);
                            ward1.SendData(response);
                             cmd = (string)jsonObject["WardNumber"] + "|" + "false";
                            // SerialPortViewModel.Instense.SendData(cmd);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Loger.Instence.SaveLog(ex.ToString());
                    }

                }
                catch (OperationCanceledException)
                {
                    Loger.Instence.SaveLog("读取操作已取消");
                }
                catch (IOException ex) when (ex.InnerException is SocketException se && se.SocketErrorCode == SocketError.Interrupted)
                {
                    Loger.Instence.SaveLog("连接被安全终止");
                }
                catch (Exception ex)
                {
                    Loger.Instence.SaveLog(ex.Message);
                }
            }
        }

        bool isReturn = false;
        public string ReadClient(TcpClient client)
        {
            var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            var stream = client.GetStream();
            var buffer = new byte[1024];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ListNurseClient.Remove(ListNurseClient.FirstOrDefault(x => x.Client == client));
                    ListWardClient.Remove(ListWardClient.FirstOrDefault(x => x.Client == client));
                });
            }
            var message = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            message = message.TrimEnd('\0');
            return message;

        }


        /// <summary>
        ///  合并Json对象
        /// </summary>
        /// <param name="originalJson"></param>
        /// <param name="wardNumber"></param>
        /// <returns></returns>
        private JsonNode MergeWardData(JsonNode originalJson, string wardNumber)
        {
            try
            {
                var ward = WardInfo.Instance.LstWard
                    .FirstOrDefault(w => w.WardNumber == wardNumber);
                if (ward == null) return originalJson;

                // 深拷贝病房数据
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };
                var wardNode = JsonSerializer.SerializeToNode(ward, options)?.AsObject();
                if (wardNode == null) return originalJson;

                // 创建新JSON对象进行合并
                var mergedJson = new JsonObject();

                // 合并原始数据
                foreach (var prop in originalJson.AsObject())
                {
                    mergedJson.Add(prop.Key, JsonNode.Parse(prop.Value.ToJsonString()));
                }

                // 合并病房数据（覆盖同名属性）
                foreach (var prop in wardNode)
                {
                    mergedJson[prop.Key] = JsonNode.Parse(prop.Value.ToJsonString());
                }

                return mergedJson;
            }
            catch (Exception ex)
            {
                Loger.Instence.SaveLog($"MergeWardData Error: {ex.Message}");
                return originalJson;
            }
        }

        /// <summary>
        /// NurseConnect
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <param name="IP"></param>
        private void NurseConnect(JsonNode jsonObject, TcpClient client)
        {
            var IP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            NurseClient nurseClient = new NurseClient(jsonObject["UserName"].ToString(), IP, jsonObject["UserCard"].ToString(), true, client);
            
            nurseClient.Disconnected += (sender, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 从列表中移除断开的客户端
                    ListNurseClient.Remove(nurseClient);
                    CallDispatcher.Instance._nurses.TryRemove(nurseClient.NurseName, out _);
                });
            };
            Application.Current.Dispatcher.Invoke(() =>
            {
                ListNurseClient.Add(nurseClient);
                CallDispatcher.Instance._nurses.TryAdd(
               (string)jsonObject["UserName"],
               nurseClient

           );
                nurseClient.IsAvailable = true;
            });
           

        }
        /// <summary>
        /// WardConnect
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <param name="IP"></param>
        private void WardConnect(JsonNode jsonObject, TcpClient client)
        {
            var IP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            WardClient wardClient = new WardClient(jsonObject["UserName"].ToString(), IP, jsonObject["UserCard"].ToString(), true, client);
            Application.Current.Dispatcher.Invoke(() =>
            {
                ListWardClient.Add(wardClient);
            });
            wardClient.Disconnected += (sender, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 从列表中移除断开的客户端
                    ListWardClient.Remove(wardClient);
                    CallDispatcher.Instance._nurses.TryRemove(wardClient.WardName, out _);
                });
            };
            Task task= Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        string message = ReadClient(client);
                        if (message == null)
                            break;
                        DataRecviced?.Invoke(message);
                        Loger.Instence.SaveLog(message);
                        JsonDeserialize(message, client);
                    }
                    catch (Exception ex)
                    {
                        Loger.Instence.SaveLog(ex.ToString());
                    }
                }
            });
        }
        #region NurseLogin
        private void NurseLogin(JsonNode jsonObject, TcpClient client)
        {
            if (SelectUser((string)jsonObject["UserName"], (string)jsonObject["Password"]))
            {
                Stream stream = client.GetStream();
                try
                {
                    stream.Write(Encoding.UTF8.GetBytes("{\"DataMethod\":\"NurseLogin\",\"Result\":\"true\"}"), 0, Encoding.UTF8.GetBytes("{\"DataMethod\":\"NurseLogin\",\"Result\":\"true\"}").Length);
                }
                catch (Exception ex)
                {
                    Loger.Instence.SaveLog(ex.ToString());
                }
            }
        }
        private bool SelectUser(string Username, string Password)
        {
            string sqlConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=MyDataBase;Integrated Security=true;";
            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT * FROM Users WHERE Username = @Username AND Password = @Password";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", Username);
                        command.Parameters.AddWithValue("@Password", Password);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return true;
                            }
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Loger.Instence.SaveLog(ex.Message);
                }
                return false;
            }
        }
        #endregion
        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="action"></param>
        private void OnPropertyChanged(NotifyCollectionChangedAction action = NotifyCollectionChangedAction.Reset)
        {
            CollectionChanged?.Invoke(
                this,
                new NotifyCollectionChangedEventArgs(action) // 根据动作类型传递参数
            );
        }

    }


}
