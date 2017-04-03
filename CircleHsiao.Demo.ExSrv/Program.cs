using System;
using System.Runtime.InteropServices;
using System.Threading;
using Ptc.iPos.SignalR.Server;

namespace SignalR.Server
{
    internal class Program
    {
        #region DllImport

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]   //找子窗体
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]   //用于发送信息给窗体
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        [DllImport("User32.dll", EntryPoint = "ShowWindow")]   //
        private static extern bool ShowWindow(IntPtr hWnd, int type);

        #endregion

        #region Method

        private static void Main(string[] args)
        {
            //檢核僅能執行一個執行個體
            bool isRun = false;
            String ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            Mutex m = new Mutex(true, ProcessName, out isRun);
            if (!isRun) return;

            Console.Title = "SignalR.ExSrv";
            IntPtr ParenthWnd = new IntPtr(0);
            IntPtr et = new IntPtr(0);
            ParenthWnd = FindWindow(null, "SignalR.ExSrv");

            ShowWindow(ParenthWnd, 1);//隐藏本dos窗体, 0: 后台执行；1:正常启动；2:最小化到任务栏；3:最大化

            SignalRService service = new SignalRService();
            Console.WriteLine("Server running on {0}", service.URL);

            while (true) {
                string input = Console.ReadLine();
                service.BrocastMsgToAll(input, "Admin");
            }
        }

        #endregion
    }
}