using System;
using System.Collections.Generic;

namespace Ptc.iPos.SignalR.Domain
{
    /// <summary>未觸發方法</summary>
    public class ShelvedMethod
    {
        #region Property

        /// <summary>方法名稱</summary>
        public string Name { get; set; }

        /// <summary>參數組合</summary>
        public List<object> MethodArgs { get; set; }

        /// <summary>應觸發時點</summary>
        public DateTime ExecAt { get; set; }

        /// <summary>確認碼</summary>
        public string CmdCode { get; set; }

        /// <summary>伺服端方法名稱(客戶端重發用)</summary>
        public string ServerSideMethodName { get; set; }

        #endregion
    }
}