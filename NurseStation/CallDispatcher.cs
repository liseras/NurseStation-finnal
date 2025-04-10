using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using WardCallSystemNurseStation;
using System.Collections.ObjectModel;
using System.Windows;

namespace NurseStation
{
   

    public enum CallStatus
    {
        Waiting, // 等待分配
        Processing, // 处理中
        Completed, // 已完成
        Timeout // 超时
    }

    public class CallDispatcher
    {
        public  BlockingCollection<CallRequest> _callQueue = new BlockingCollection<CallRequest>(); // 阻塞队列
        public List<CallRequest> _callQueueCpy = new List<CallRequest>(); // 阻塞队列
        public  ConcurrentDictionary<string, NurseClient> _nurses = new ConcurrentDictionary<string, NurseClient>(); // 护士集合
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly object _lock = new object();

        //点对点呼叫字典
        public ConcurrentDictionary<string, NurseClient> p2pDic = new ConcurrentDictionary<string, NurseClient>();
        //呼叫记录
        public ObservableCollection<CallRecord> lstCallRecord = new ObservableCollection<CallRecord>();

        #region 单例模式
        private static CallDispatcher _instance;
        private static object _Slock = new object();
        public static CallDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_Slock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CallDispatcher();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion
        public CallDispatcher()
        {
            // 启动后台任务处理呼叫
            Task.Run(ProcessCalls);
        }



        // 发起呼叫
        public void PlaceCall(CallRequest request)
        {
            lock (_lock)
            {
                _callQueue.Add(request); // 添加到阻塞队列
            }
        }

        // 核心调度逻辑
        private void ProcessCalls()
        {
            try
            {
                foreach (var call in _callQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                        if (call.Status == CallStatus.Waiting)
                        {
                            _callQueueCpy.Add(call);
                            // 查找可用护士（按最后响应时间排序）
                            var availableNurse = _nurses.Values
                                .Where(n => n.IsAvailable)
                                .OrderBy(n => n.LastResponseTime)
                                .FirstOrDefault();

                            if (availableNurse != null)
                            {
                                AssignCall(call, availableNurse);
                            }
                            else
                            {
                                // 如果没有可用护士，重新入队
                                _callQueue.Add(call);
                            }
                        }
                    
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("调度器已停止");
            }
        }

        // 分配呼叫
        private void AssignCall(CallRequest call, NurseClient nurse)
        {

            lock (_lock)
            {
                // 标记护士为忙碌
                //nurse.IsAvailable = false;

                // 更新呼叫状态
                call.Status = CallStatus.Processing;
                call.AssignedNurse = nurse;
                nurse.LastResponseTime = DateTime.Now;
                //添加呼叫关系
                bool ret =  p2pDic.TryAdd(call.WardNumber, nurse);
                if(!ret)
                {
                    MessageBox.Show($"TryAdd {nurse.NurseName} failer");
                }
                
            }
        }

        // 护士响应处理
        public bool HandleNurseResponse(string nurseName, bool isSuccess)
        {
            lock (_lock)
            {
                try
                {
                    bool IsResult = false;
                    if (_nurses.TryGetValue(nurseName, out var nurse))
                    {
                        // 标记护士为可用
                        
                        //在接收状态时设置
                        nurse.LastResponseTime = DateTime.Now;

                        // 查找对应的呼叫
                        var call = _callQueueCpy.FirstOrDefault(c =>
                            c.AssignedNurse?.NurseName == nurseName &&
                            c.Status == CallStatus.Processing);
                        _callQueueCpy.Remove(call);

                        p2pDic.TryRemove(call.WardNumber, out nurse);
                        if (call != null)
                        {
                            if (isSuccess)
                            {
                                nurse.IsAvailable = true;
                                call.Status = CallStatus.Completed;
                                // 记录呼叫记录
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    lstCallRecord.Add(new CallRecord(call.CallTime, call.WardNumber, call.PatientName, nurseName, "已接听"));
                                });
                                //添加呼叫记录到数据库
                                CallRecordRepository.Instance.InsertCallRecord(new CallRecord(call.CallTime, call.WardNumber, call.PatientName, nurseName, "已接听"));
                                // 从字典中移除该呼叫
                                p2pDic.TryRemove(call.WardNumber, out nurse);
                                IsResult = true;
                            }
                            else
                            {
                                nurse.IsAvailable = true;
                                call.Status = CallStatus.Timeout;
                                var availableNurse = _nurses.Values
                                    .Where(n => n.IsAvailable)
                                    .OrderBy(n => n.LastResponseTime)
                                    .FirstOrDefault();
                                if (availableNurse == null)
                                {
                                    IsResult = true;
                                    
                                    // 记录未接听的记录
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        lstCallRecord.Add(new CallRecord(call.CallTime, call.WardNumber, call.PatientName, nurseName, "未接听"));
                                    });
                                    // 记录到数据库
                                    CallRecordRepository.Instance.InsertCallRecord(new CallRecord(call.CallTime, call.WardNumber, call.PatientName, nurseName, "未接听"));
                                }
                                else
                                    _callQueue.Add(call); // 重新入队

                            }
                        }
                    }
                    return IsResult;
                }
                catch(Exception ex)
                {
                    Loger.Instence.SaveLog(ex.ToString());
                    return false;
                }
            }
        }


        // 停止调度器
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _callQueue.CompleteAdding();
        }
    }

    public class CallRequest
    {
        // 基本信息
        public string WardNumber { get; set; }  // 病房号
        public DateTime CallTime { get; set; }  // 呼叫时间
        public string PatientName { get; set; } // 患者姓名

        // 状态管理
        public CallStatus Status { get; set; } = CallStatus.Waiting; // 当前状态
        public NurseClient AssignedNurse { get; set; } // 分配的护士
     

        // 扩展字段（可选）
        public int Priority { get; set; } = 1; // 呼叫优先级（默认为1，数值越小优先级越高）
       
        
    }
}
