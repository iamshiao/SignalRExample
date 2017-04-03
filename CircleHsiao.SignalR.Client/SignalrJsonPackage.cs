using System.Collections.Generic;

namespace Ptc.iPos.SignalR.Client
{
    /// <summary>接收與反序列化 SignalR Json 命令用的類別</summary>
    public class SignalrJsonPackage
    {
        /// <summary>伺服器名稱</summary>
        public string H { get; set; }

        /// <summary>方法名稱</summary>
        public string M { get; set; }

        /// <summary>參數</summary>
        public List<object> A { get; set; }
    }
}