using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using CircleHsiao.Extensions;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using Ptc.iPos.SignalR.Domain;

namespace Ptc.iPos.SignalR.Client
{
    /// <summary>SignalRClient</summary>
    public class SignalRClient : ISignalRClient
    {
        #region Constructor

        /// <summary>SignalRClient</summary>
        public SignalRClient()
        {
        }

        /// <summary>SignalRClient</summary>
        /// <param name="hostUrl">Server Url</param>
        /// <param name="hubName">hubName</param>
        /// <param name="validCode">validCode</param>
        /// <param name="name">名稱</param>
        /// <param name="groupNames">群組名稱</param>
        public SignalRClient(string hostUrl, string hubName, string validCode, string name, List<string> groupNames = null)
        {
            HostUrl = hostUrl;
            ServerHubName = hubName;
            ValidECode = validCode;
            Name = name;
            if (groupNames != null && groupNames.Any()) {
                GroupNames = groupNames;
            }
        }

        #endregion

        #region Property

        /// <summary>PosNo or "SC"</summary>
        public string ValidECode { get; set; }

        /// <summary>服務名或方法名</summary>
        public string Name { get; set; }

        /// <summary>伺服器位置</summary>
        public string HostUrl { get; set; }

        /// <summary>伺服器名稱</summary>
        public string ServerHubName { get; set; } = @"SignalRServerHub";

        /// <summary>所屬群組</summary>
        public List<string> GroupNames { get; set; } = new List<string>();

        /// <summary>SignalR Client HubProxy</summary>
        public IHubProxy HubProxy { get; private set; } = null;

        /// <summary>SignalR Client HubConn</summary>
        public HubConnection HubConn { get; private set; } = null;

        /// <summary>傳遞參數用查詢字串</summary>
        public Dictionary<string, string> QueryStrs { get; set; } = new Dictionary<string, string>();

        /// <summary>未執行(伺服器未確認)的方法佇列</summary>
        public List<ShelvedMethod> MethodQueue { get; set; } = new List<ShelvedMethod>();

        /// <summary>方法佇列 XML 記錄檔目錄</summary>
        public string XmlDir { get; set; }

        /// <summary>方法佇列備份檔目錄</summary>
        public string BakDir { get; set; }

        /// <summary>當前連線狀態</summary>
        public ConnectionState CurrState { get; set; }

        #endregion

        #region Client control

        /// <summary>讀取或附予 INI 預設檔案路徑，並轉換相對目錄為絕對</summary>
        private void InitDir()
        {
            // 由 INI 讀取 XML 目錄，若不存在則寫入在預設目錄
            INI ini = new INI();
            XmlDir = ini.Read("SignalR", "ClientXmlDir");
            if (string.IsNullOrWhiteSpace(XmlDir)) {
                XmlDir = System.AppDomain.CurrentDomain.BaseDirectory + @"Data\ClientEvent\";
            }

            // 由 INI 讀取備份檔目錄，若不存在則寫入在預設目錄
            BakDir = ini.Read("SignalR", "ClientBakDir");
            if (string.IsNullOrWhiteSpace(BakDir)) {
                BakDir = System.AppDomain.CurrentDomain.BaseDirectory + @"Data\ClientEvent\";
            }
        }

        /// <summary>從 XML 回復客戶端狀態</summary>
        public void RestoreFromXML()
        {
            // 初始化 XML 與備份檔目錄設定
            InitDir();

            List<ShelvedMethod> history = null;
            XmlSerializer xs = new XmlSerializer(typeof(List<ShelvedMethod>));
            if (File.Exists(XmlDir + $"{ValidECode}{Name}.xml")) {
                try {
                    using (var sr = new StreamReader(XmlDir + $"{ValidECode}{Name}.xml")) {
                        history = (List<ShelvedMethod>)xs.Deserialize(sr);
                    }
                }
                catch (Exception ex) {
                    string exMsg = ex.Message;
                    File.Copy(XmlDir + $"{ValidECode}{Name}.xml", XmlDir + $"{ValidECode}{Name}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xml");
                    File.Delete(XmlDir + $"{ValidECode}{Name}.xml");
                    Thread.Sleep(100);
                    SaveMethodQueueToXML();
                    using (var sr = new StreamReader(XmlDir + $"{ValidECode}{Name}.xml")) {
                        history = (List<ShelvedMethod>)xs.Deserialize(sr);
                    }
                }
            }
            else {
                SaveMethodQueueToXML();
            }

            if (history != null && history.Any()) {
                MethodQueue = history;
            }
        }

        /// <summary>儲存未確認接收的方法佇列</summary>
        public void SaveMethodQueueToXML()
        {
            // 初始化 XML 與備份檔目錄設定
            InitDir();

            // 避免檔損先作備份
            try {
                Directory.CreateDirectory(XmlDir);
                Directory.CreateDirectory(BakDir);

                // 做新備份
                File.Copy(XmlDir + $"{ValidECode}{Name}.xml", BakDir + $"{ValidECode}{Name}{DateTime.Now.ToString("yyyyMMddHHmmss")}.bak");
                // 刪除舊備份
                if (string.IsNullOrWhiteSpace(BakDir)) {
                    BakDir = Environment.CurrentDirectory;
                }

                DirectoryInfo di = new DirectoryInfo(BakDir);
                List<FileInfo> allBaks = di.GetFiles($"{ValidECode}{Name}*.bak").ToList();
                FileInfo newestBak = allBaks.OrderByDescending(f => f.CreationTime).FirstOrDefault();
                allBaks.Where(bak => bak.Name != newestBak.Name).ToList().ForEach(bak => File.Delete(bak.FullName));
            }
            catch (Exception ex) {
                string exMsg = ex.Message;
            }

            // 將目前的方法佇列寫進 XML
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ShelvedMethod>));
                using (StreamWriter writer = new StreamWriter(XmlDir + $"{ValidECode}{Name}.xml")) {
                    serializer.Serialize(writer, MethodQueue);
                }
            }
            catch (Exception ex) {
                string exMsg = ex.Message;
            }
        }

        /// <summary>刪除舊於特定時間的佇列方法</summary>
        /// <param name="date"></param>
        public void ClearShelvedMethodLessThanDate(DateTime date)
        {
            // 初始化 XML 與備份檔目錄設定
            InitDir();

            List<ShelvedMethod> history = null;
            XmlSerializer xs = new XmlSerializer(typeof(List<ShelvedMethod>));
            if (File.Exists(XmlDir + $"{ValidECode}{Name}.xml")) {
                using (var sr = new StreamReader(XmlDir + $"{ValidECode}{Name}.xml")) {
                    history = (List<ShelvedMethod>)xs.Deserialize(sr);
                }
            }
            else {
                SaveMethodQueueToXML();
            }

            if (history != null && history.Any()) {
                history = history.Where(m => m.ExecAt > date).ToList();
            }

            // 將目前的方法佇列寫進 XML
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ShelvedMethod>));
                using (StreamWriter writer = new StreamWriter(XmlDir + $"{ValidECode}{Name}.xml")) {
                    serializer.Serialize(writer, history);
                }
            }
            catch (Exception ex) {
                string exMsg = ex.Message;
            }
        }

        /// <summary>重送方法佇列中所有擱置命令</summary>
        public void ResendMethodInQueue()
        {
            List<ShelvedMethod> queue = new List<ShelvedMethod>(MethodQueue); // 未避免待會刪到當前狀態記錄，要 Clone
            MethodQueue.Clear(); // 清掉客戶端狀態中的佇列方法，因為觸發時會再寫入。
            SaveMethodQueueToXML();

            queue.ForEach(m => HubProxy.Invoke(m.ServerSideMethodName, m.Name, m.MethodArgs.ToArray()));
        }

        /// <summary>寫入重發佇列</summary>
        /// <param name="methodName">方法名</param>
        /// <param name="methodArgs">參數</param>
        /// <param name="serverSideMethodName">伺服端方法名稱(客戶端重發用)</param>
        /// <returns>確認碼</returns>
        public string RecordMethodQueue(string methodName, List<object> methodArgs, string serverSideMethodName = "")
        {
            ShelvedMethod sm = null;
            if (MethodQueue.Any(m => m.Name == methodName && !m.MethodArgs.Except(methodArgs).Any())) {
                sm = MethodQueue.First(m => m.Name == methodName && !m.MethodArgs.Except(methodArgs).Any());
                sm.ExecAt = DateTime.Now;
            }
            else {
                // 原理 有重發需求的方法應在此記錄，並產生確認碼並隨方法送回伺服器，伺服器收到後會將確認碼(CmdCode)回傳回來 客戶端在重新收到確認碼之後才把對應的 ShelvedMethod 清掉 重發的時機則是在狀態改變為已連線時一次重發
                sm = new ShelvedMethod
                {
                    Name = methodName,
                    MethodArgs = methodArgs,
                    ExecAt = DateTime.Now,
                    CmdCode = Guid.NewGuid().ToString(),
                    ServerSideMethodName = serverSideMethodName
                };
                MethodQueue.Add(sm);
            }

            SaveMethodQueueToXML();

            return sm.CmdCode;
        }

        /// <summary>設定連線資訊</summary>
        /// <param name="groupNames">設定所屬群組，無輸入則無群組</param>
        public virtual void SetConnection(List<string> groupNames = null)
        {
            if (groupNames != null && groupNames.Any()) {
                GroupNames = groupNames;
                QueryStrs["JsonGroupNames"] = JsonConvert.SerializeObject(groupNames);
            }
            QueryStrs["ValidECode"] = ValidECode;
            QueryStrs["Name"] = Name;

            HubConn = new HubConnection(HostUrl, QueryStrs);
            HubProxy = HubConn.CreateHubProxy(ServerHubName);

            //這邊的擴充可以在子類別覆寫
            HubProxy.On<string>(
                "CmdFinished", (cmdCode) => {
                    CmdFinished(cmdCode);
                });

            HubProxy.On<string, List<object>, string>(
                "ExecDynamicCmd", (methodName, args, cmdCode) => {
                    ExecDynamicCmd(methodName, args);
                });

            HubConn.StateChanged += OnStateChanged;
            HubConn.Error += OnError;
            HubConn.Received += OnReceived;
        }

        /// <summary>主動連接</summary>
        public virtual void Connect(List<string> groupNames = null)
        {
            if (string.IsNullOrWhiteSpace(HostUrl)) {
                INI ini = new INI();
                HostUrl = ini.Read("SignalR", "ServerURL");
                if (string.IsNullOrWhiteSpace(HostUrl)) {
                    throw new Exception("SignalR 伺服器 IP 位置未正確設置，請檢查 POSProfile.INI 下 [SignalR] 中 ServerURL=... 的設定。");
                }
            }

            RestoreFromXML(); // 先從 XML 讀回擱置的命令
            SetConnection(groupNames);
            // 連接失敗時，30秒後重試連接
            HubConn.Start().ContinueWith(t => {
                if (t.IsFaulted) {
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                    Connect();
                }
            });
        }

        /// <summary>主動斷線</summary>
        public virtual void SelfDisconnect()
        {
            HubConn.Stop();
        }

        #endregion

        #region SignalR communication methods

        #region To server

        /// <summary>加入群組</summary>
        /// <param name="groupName">群組名稱</param>
        public void JoinGroup(string groupName)
        {
            List<object> args = new List<object> { groupName };
            string cmdCode = RecordMethodQueue("JoinGroup", args);
            if (CurrState == ConnectionState.Connected) {
                HubProxy.Invoke("JoinGroup", groupName, cmdCode);
            }
        }

        /// <summary>離開群組</summary>
        /// <param name="groupName">群組名稱</param>
        public void LeaveGroup(string groupName)
        {
            List<object> args = new List<object> { groupName };
            string cmdCode = RecordMethodQueue("LeaveGroup", args);
            if (CurrState == ConnectionState.Connected) {
                HubProxy.Invoke("LeaveGroup", groupName, cmdCode);
            }
        }

        #endregion

        #region From server

        /// <summary>伺服器告知清除命令</summary>
        /// <param name="msg">確認碼</param>
        public void CmdFinished(string cmdCode)
        {
            MethodQueue.RemoveAll(mq => mq.CmdCode == cmdCode);
            SaveMethodQueueToXML();
        }

        /// <summary>遠端透過本方法可直接動態呼叫本地方法</summary>
        /// <param name="methodName">方法名稱</param>
        /// <param name="args">引數</param>
        public virtual void ExecDynamicCmd(string methodName, List<object> args)
        {
            // 若 args 型別特殊或需其他特別處理請繼承此類別後覆寫本方法
            try {
                MethodInfo method = this.GetType().GetMethod(methodName, // 在本類別中尋找符合的方法
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                method?.Invoke(this, args.ToArray());
            }
            catch (Exception ex) {
                string exMsg = ex.Message;
            }
        }

        #endregion

        #endregion

        #region SignalR connection events

        protected virtual void OnStateChanged(StateChange s)
        {
            CurrState = s.NewState;
            Console.WriteLine($"{s.NewState}.");
            if (s.NewState == ConnectionState.Connected) {
                ResendMethodInQueue();
                Console.WriteLine(HubConn.ConnectionId);
            }
        }

        protected virtual void OnError(Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}.");
        }

        /// <summary>收到任何命令都會觸發本事件</summary>
        /// <param name="jsonPak">事件資訊的 json 物件</param>
        protected virtual void OnReceived(string jsonPak)
        {
            // 收到需要確認的命令時(參數包含 "cmdCode:"字樣)，需回報收到命令讓伺服器把方法佇列中該方法清掉。
            SignalrJsonPackage pak = JsonConvert.DeserializeObject<SignalrJsonPackage>(jsonPak);
            string cmdCode = pak.A.FirstOrDefault(param => param.ToString().Contains("cmdCode:"))?.ToString().Replace("cmdCode:", "");
            if (!string.IsNullOrEmpty(cmdCode) && CurrState == ConnectionState.Connected) {
                HubProxy.Invoke("CmdFinished", Name, ValidECode, cmdCode);
            }
        }

        #endregion
    }
}