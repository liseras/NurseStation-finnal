using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NurseStation
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private List<Task> _backgroundTasks = new List<Task>();

        public App()
        {
            // 全局异常处理（防止崩溃后进程残留）
            DispatcherUnhandledException += (s, ex) =>
            {
                ex.Handled = true;
                _cts.Cancel(); // 异常时触发取消 [[6]]
                Shutdown();
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            CleanupResources();
            TerminateBackgroundTasks();
            ForceGarbageCollection();
        }

        private void CleanupResources()
        {
            foreach (Window window in Windows)
            {
                if (window is IDisposable disposable)
                    disposable.Dispose(); // 释放非托管资源 [[7]]

                
            }
        }

        private void TerminateBackgroundTasks()
        {
            _cts.Cancel(); // 发送取消信号 [[9]]
            Task.WaitAll(_backgroundTasks.ToArray(), 3000); // 最多等待3秒
        }

        private void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers(); // 确保资源回收 [[4]]
        }

      
    }
}
