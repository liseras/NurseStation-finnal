using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  WardCallSystemNurseStation

{
    public class Loger
    {
        #region Singleton
        private static object locker = new object();
        private static Loger _instence = null;
        public static Loger Instence
        {
            get
            {
                if (_instence == null)
                {
                    lock (locker)
                    {
                        if (_instence == null)
                        {
                            _instence = new Loger();
                        }
                    }
                }
                return _instence;
            }
        }
        #endregion

        /// <summary>
        /// 保存日志
        /// </summary>
        /// <param name="log"></param>

        public void SaveLog(string log)
        {
            string path = "Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            string filePath = path + "\\" + fileName;
            File.AppendAllText(filePath, log + DateTime.Now.ToString("yyyy-MM-dd") + "\r\n");
        }




    }
}
