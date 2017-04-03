using System.Collections.Generic;
using Ptc.iPos.SignalR.Domain;

namespace Ptc.iPos.SignalR.Server
{
    /// <summary>客戶端狀態資訊</summary>
    public class ClientInfo
    {
        /// <summary>SignalR ID</summary>
        public string GUID { get; set; }

        /// <summary>PosNo or "SC"</summary>
        public string ValidECode { get; set; }

        /// <summary>服務名或方法名</summary>
        public string Name { get; set; }

        /// <summary>所屬群組</summary>
        public List<string> GroupNames { get; set; }

        /// <summary>是否正在連線</summary>
        public bool IsConnect { get; set; }

        /// <summary>未觸發方法佇列</summary>
        public List<ShelvedMethod> MethodQueue { get; set; } = new List<ShelvedMethod>();
    }
}