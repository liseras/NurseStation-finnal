using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using WardCallSystemNurseStation;
using NurseStation;

namespace NurseStation
{
    class NurseMessageListener
    {
        private readonly BlockingCollection<KeyValuePair<NurseClient, byte[]>> _messageQueue = new BlockingCollection<KeyValuePair<NurseClient, byte[]>>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public void StartListening()
        {
            // 消息接收线程
            Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        var message = _messageQueue.Take(_cts.Token);
                        ProcessMessage(message.Key, message.Value);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, _cts.Token);
        }

        public void StopListening()
        {
            _cts.Cancel();
            _messageQueue.CompleteAdding();
        }
        public void EnqueueMessage(NurseClient nurse, byte[] data)
        {
            _messageQueue.Add(new KeyValuePair<NurseClient, byte[]>(nurse, data));
        }

        private void ProcessMessage(NurseClient nurse, byte[] rawData)
        {
            try
            {
                string message = Encoding.UTF8.GetString(rawData).Trim('\0');
                JsonNode json = JsonNode.Parse(message);

                // 更新护士最后响应时间
                nurse.LastResponseTime = DateTime.Now;

                // 处理呼叫响应
                if (json["DataMethod"]?.ToString() == "CallResponse")
                {
                    CallDispatcher.Instance.HandleNurseResponse(
                        json["NurseName"]?.ToString(),
                        json["IsSuccess"]?.ToString() == "True"
                    );
                }
            }
            catch (Exception ex)
            {
                Loger.Instence.SaveLog($"消息处理失败: {ex.Message}");
            }
        }
    }
}
