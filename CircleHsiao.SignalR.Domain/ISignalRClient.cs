using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Client;

namespace Ptc.iPos.SignalR.Domain
{
    /// <summary>ISignalRClient</summary>
    public interface ISignalRClient
    {
        #region Property

        /// <summary>PosNo or "SC"</summary>
        string ValidECode { get; set; }

        /// <summary>服務名或方法名</summary>
        string Name { get; set; }

        /// <summary>伺服器位置</summary>
        string HostUrl { get; set; }

        /// <summary>伺服器名稱</summary>
        string ServerHubName { get; set; }

        /// <summary>所屬群組</summary>
        List<string> GroupNames { get; set; }

        /// <summary>SignalR Client HubProxy</summary>
        IHubProxy HubProxy { get; }

        /// <summary>SignalR Client HubConn</summary>
        HubConnection HubConn { get; }

        /// <summary>傳遞參數用查詢字串</summary>
        Dictionary<string, string> QueryStrs { get; set; }

        /// <summary>未執行(伺服器未確認)的方法佇列</summary>
        List<ShelvedMethod> MethodQueue { get; set; }

        /// <summary>方法佇列 XML 記錄檔目錄</summary>
        string XmlDir { get; set; }

        /// <summary>方法佇列備份檔目錄</summary>
        string BakDir { get; set; }

        #endregion

        #region Method

        /// <summary>設定連線資訊</summary>
        /// <param name="groupName">設定所屬群組，無輸入則無群組</param>
        void SetConnection(List<string> groupName = null);

        /// <summary>連接</summary>
        /// <param name="groupName">設定所屬群組，無輸入則無群組</param>
        void Connect(List<string> groupName = null);

        /// <summary>當伺服器完成命令，經此告知可清除該命令</summary>
        /// <param name="msg">確認碼</param>
        void CmdFinished(string cmdCode);

        /// <summary>儲存未確認接收的方法佇列</summary>
        void SaveMethodQueueToXML();

        /// <summary>寫入重發佇列</summary>
        /// <param name="methodName">方法名</param>
        /// <param name="methodArgs">參數</param>
        /// <param name="serverSideMethodName">伺服端方法名稱(客戶端重發用)</param>
        /// <returns>確認碼</returns>
        string RecordMethodQueue(string methodName, List<object> methodArgs, string serverSideMethodName = "");

        /// <summary>重送方法佇列中所有擱置命令</summary>
        void ResendMethodInQueue();

        /// <summary>從 XML 回復客戶端狀態</summary>
        void RestoreFromXML();

        /// <summary>遠端透過本方法可直接動態呼叫本地方法</summary>
        /// <param name="methodName">方法名稱</param>
        /// <param name="args">引數</param>
        void ExecDynamicCmd(string methodName, List<object> args);

        #endregion
    }
}