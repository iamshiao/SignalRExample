using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CircleHsiao.Extensions;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Ptc.iPos.SignalR.Domain;

namespace Ptc.iPos.SignalR.Server
{
    public class SignalRServerHub : Hub
    {
        #region Property & Constructor

        /// <summary>客戶端狀態</summary>
        internal static List<ClientInfo> CurrClients = new List<ClientInfo>();

        /// <summary>客戶端狀態 XML 記錄檔目錄</summary>
        private string XmlDir { get; set; }

        /// <summary>客戶端狀態備份檔目錄</summary>
        private string BakDir { get; set; }

        /// <summary>Server 所在網址</summary>
        public string URL { get; set; }

        public SignalRServerHub()
        {
        }

        #endregion

        #region Inner methods

        /// <summary>讀取或附予 INI 預設檔案路徑，並轉換相對目錄為絕對</summary>
        private void InitDir()
        {
            // 由 INI 讀取 XML 目錄，若不存在則寫入在預設目錄
            INI ini = new INI();
            XmlDir = ini.Read("SignalR", "ServerXmlDir");
            if (string.IsNullOrWhiteSpace(XmlDir)) {
                XmlDir = System.AppDomain.CurrentDomain.BaseDirectory + @"ClientEvent\";
            }

            // 由 INI 讀取備份檔目錄，若不存在則寫入在預設目錄
            BakDir = ini.Read("SignalR", "ServerBakDir");
            if (string.IsNullOrWhiteSpace(BakDir)) {
                BakDir = System.AppDomain.CurrentDomain.BaseDirectory + @"ClientEvent\";
            }
        }

        /// <summary>從 XML 回復客戶端狀態</summary>
        private void RestoreFromXML()
        {
            // 初始化 XML 與備份檔目錄設定
            InitDir();

            // 讀取 XML
            List<ClientInfo> history = null;
            XmlSerializer xs = new XmlSerializer(typeof(List<ClientInfo>));
            if (File.Exists(XmlDir + "ClientStatus.xml")) {
                using (var sr = new StreamReader(XmlDir + "ClientStatus.xml")) {
                    history = (List<ClientInfo>)xs.Deserialize(sr);
                }
            }
            else { // 如果 XML 不存在(首次執行)，建空檔
                SaveStateToXML();
            }

            if (history != null && history.Any()) {
                CurrClients = history; // 從 XML 讀進當前狀態
            }
        }

        /// <summary>將目前的客戶端狀態寫進 XML</summary>
        private void SaveStateToXML()
        {
            // 初始化 XML 與備份檔目錄設定
            InitDir();

            // 避免檔損先作備份
            try {
                Directory.CreateDirectory(XmlDir);
                Directory.CreateDirectory(BakDir);
                // 做新備份，備份檔刪除中間日期值與變更附檔名回 xml 即可取代當前
                File.Copy(XmlDir + "ClientStatus.xml", BakDir + $"ClientStatus{DateTime.Now.ToString("yyyyMMddHHmmss")}.bak");

                // 刪除舊備份
                if (string.IsNullOrWhiteSpace(BakDir))
                    BakDir = Environment.CurrentDirectory;
                DirectoryInfo di = new DirectoryInfo(BakDir);
                List<FileInfo> allBaks = di.GetFiles("ClientStatus*.bak").ToList();
                FileInfo newestBak = allBaks.OrderByDescending(f => f.CreationTime).FirstOrDefault();
                allBaks.Where(bak => bak.Name != newestBak.Name).ToList().ForEach(bak => File.Delete(bak.FullName));
            }
            catch (Exception ex) {
                string exMsg = ex.Message;
            }

            // 將目前的客戶端狀態寫進 XML
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ClientInfo>));
                using (StreamWriter writer = new StreamWriter(XmlDir + "ClientStatus.xml")) {
                    serializer.Serialize(writer, CurrClients);
                }
            }
            catch (Exception ex) {
                string exMsg = ex.Message;
            }
        }

        /// <summary>尋找傳入的名稱與識別是否已存在於曾連線客戶端組合中</summary>
        /// <param name="name">名稱</param>
        /// <param name="vCode">識別</param>
        /// <returns>客戶端資訊</returns>
        private ClientInfo FindIfClientExist(string name, string vCode)
        {
            ClientInfo ret = null;
            ret = CurrClients.FirstOrDefault(cc => cc.Name == name && cc.ValidECode == vCode);
            return ret;
        }

        /// <summary>寫入重發標記</summary>
        /// <param name="client">目標客戶端</param>
        /// <param name="methodName">方法名</param>
        /// <returns>"cmdCode:確認碼"</returns>
        private string RecordMethodQueue(ClientInfo client, string methodName, List<object> methodArgs)
        {
            ShelvedMethod sm = null;
            if (client.MethodQueue.Any(m => m.Name == methodName && !m.MethodArgs.Except(methodArgs).Any())) {
                sm = client.MethodQueue.First(m => m.Name == methodName && !m.MethodArgs.Except(methodArgs).Any());
                sm.ExecAt = DateTime.Now;
            }
            else {
                // 原理 所有 RecordMethodQueue 目標客戶端都會將方法名在 ClientInfo 中加入為 ShelvedMethod 收到的客戶端會將確認碼(CmdCode)回傳回來，伺服器在得到確認碼之後才把對應的 ShelvedMethod
                // 清掉 重發的時機則是在 OnReconnect 或 OnConnect 觸發時，跑一輪該客戶端所有的 ShelvedMethod
                sm = new ShelvedMethod
                {
                    Name = methodName,
                    MethodArgs = methodArgs,
                    ExecAt = DateTime.Now,
                    CmdCode = Guid.NewGuid().ToString()
                };

                client.MethodQueue.Add(sm);
            }

            // 這邊的 "cmdCode:" 是因為出去的 JSON 格式在客戶端只有讀出值而不是 KeyValPair 這類的 所以先這樣做，還沒想到比較好的辦法
            return "cmdCode:" + sm.CmdCode;
        }

        /// <summary>取得目前離線的所有(/群組中)客戶端</summary>
        /// <param name="groupName">群組名稱</param>
        /// <returns>目前離線的所有(/群組中)客戶端</returns>
        private List<ClientInfo> GetDisconnOnes(string groupName = null)
        {
            if (string.IsNullOrEmpty(groupName)) {
                return CurrClients.Where(client => !client.IsConnect).ToList();
            }
            else {
                return CurrClients.Where(client => !client.IsConnect && client.GroupNames.Contains(groupName)).ToList();
            }
        }

        #endregion

        #region Communication method

        #region From client

        /// <summary>當客戶端完成命令後主動上來清命令佇列</summary>
        /// <param name="name">名稱</param>
        /// <param name="vCode">PosNo or "SC"</param>
        /// <param name="cmdCode">確認碼</param>
        public void CmdFinished(string name, string vCode, string cmdCode)
        {
            Console.WriteLine($"Clean {vCode}{name}, {cmdCode}");
            CurrClients.First(cc => cc.Name == name && cc.ValidECode == vCode)
                .MethodQueue?.RemoveAll(mq => mq.CmdCode == cmdCode);

            SaveStateToXML();
        }

        /// <summary>客戶端主動加入群組</summary>
        /// <param name="groupName">群組名稱</param>
        public void JoinGroup(string groupName, string cmdCode)
        {
            // 自行管理的群組定義
            string connId = Context.ConnectionId;
            ClientInfo ci = CurrClients.FirstOrDefault(cc => cc.GUID == connId);
            ci.GroupNames.Add(groupName);
            // SignalR 的群組機制
            Groups.Add(connId, groupName);

            SaveStateToXML();
            Console.WriteLine($"{ci?.ValidECode}{ci?.Name} joined {groupName}");

            ReceivedCmd(cmdCode);
        }

        /// <summary>客戶端主動離開群組</summary>
        /// <param name="groupName">群組名稱</param>
        public void LeaveGroup(string groupName, string cmdCode)
        {
            // 自行管理的群組定義
            string connId = Context.ConnectionId;
            ClientInfo ci = CurrClients.FirstOrDefault(cc => cc.GUID == connId);
            ci.GroupNames.RemoveAll(og => og == groupName);
            // SignalR 的群組機制
            Groups.Remove(connId, groupName);

            SaveStateToXML();
            Console.WriteLine($"{ci?.ValidECode}{ci?.Name} left {groupName}");

            ReceivedCmd(cmdCode);
        }

        #endregion

        #region To client

        /// <summary>通知客戶端收到命令</summary>
        /// <param name="cmdCode">識別碼</param>
        private void ReceivedCmd(string cmdCode)
        {
            Clients.Caller.CmdFinished(cmdCode);
        }

        /// <summary>重送客戶端方法佇列中所有擱置命令</summary>
        /// <param name="ci">指定客戶端</param>
        public void ResendMethodInQueue(ClientInfo ci)
        {
            List<ShelvedMethod> queue = new List<ShelvedMethod>(ci.MethodQueue); // 未避免待會刪到當前狀態記錄，要 Clone
            ci.MethodQueue.Clear(); // 清掉客戶端狀態中的佇列方法，因為觸發時會再寫入。
            SaveStateToXML();

            queue.ForEach(m => DynamicCmdToClient(m.Name, m.MethodArgs));
        }

        /// <summary>直接動態呼叫所有客戶端執行某方法</summary>
        /// <param name="methodName">方法名稱</param>
        /// <param name="args">引數</param>
        /// <param name="cmdCode">客戶端完成驗證碼</param>
        /// <param name="includeMe">是否包含呼叫者本身</param>
        public void DynamicCmdToAll(string methodName, List<object> args, string cmdCode, bool includeMe)
        {
            List<ClientInfo> toClients = null;
            if (!includeMe) {// 預設不包含本身
                toClients = CurrClients.Where(cc => cc.GUID != Context.ConnectionId).ToList();
            }

            // 因為要每個都要產生確認碼的關係，所以要個別發
            if (toClients != null && toClients.Count > 0) {
                foreach (ClientInfo ci in toClients) {
                    string cmdCodeStr = RecordMethodQueue(ci, methodName, args);
                    Clients.Client(ci.GUID).ExecDynamicCmd(methodName, args, cmdCodeStr);
                }
            }

            SaveStateToXML();
            ReceivedCmd(cmdCode); // 告知客戶端完成
        }

        /// <summary>直接動態呼叫特定群組執行某方法</summary>
        /// <param name="methodName">方法名稱</param>
        /// <param name="args">引數</param>
        /// <param name="groupName">群組</param>
        /// <param name="cmdCode">客戶端完成驗證碼</param>
        /// <param name="includeMe">是否包含呼叫者本身</param>
        public void DynamicCmdToGroup(string methodName, List<object> args, string groupName, string cmdCode, bool includeMe)
        {
            Console.WriteLine($"DynamicCmdToGroup name:{methodName}, gName: {groupName}");
            List<ClientInfo> toGroup = CurrClients.Where(cc => cc.GroupNames.Contains(groupName)).ToList();
            if (!includeMe) {// 預設不包含本身
                toGroup = toGroup.Where(cc => cc.GUID != Context.ConnectionId).ToList();
            }

            // 因為要每個都要產生確認碼的關係，所以要個別發
            if (toGroup != null && toGroup.Count > 0) {
                foreach (ClientInfo ci in toGroup) {
                    string cmdCodeStr = RecordMethodQueue(ci, methodName, args);
                    Clients.Client(ci.GUID).ExecDynamicCmd(methodName, args, cmdCodeStr);
                }
            }

            SaveStateToXML();
            ReceivedCmd(cmdCode); // 告知客戶端完成
        }

        /// <summary>直接動態呼叫指定客戶端方法</summary>
        /// <param name="methodName">方法名稱</param>
        /// <param name="args">引數</param>
        /// <param name="name">服務名稱</param>
        /// <param name="validECode">PosNo or "SC"</param>
        /// <param name="cmdCode">客戶端完成驗證碼</param>
        public void DynamicCmdToClient(string methodName, List<object> args, string validECode, string name, string cmdCode)
        {
            ClientInfo toClient = CurrClients.FirstOrDefault(cc => cc.ValidECode == validECode && cc.Name == name);
            string cmdCodeStr = RecordMethodQueue(toClient, methodName, args);
            Clients.Client(toClient.GUID).ExecDynamicCmd(methodName, args, cmdCodeStr);

            SaveStateToXML();
            ReceivedCmd(cmdCode); // 告知客戶端完成
        }

        /// <summary>直接動態呼叫客戶端方法(此多載為重送機制推動)</summary>
        /// <param name="methodName">方法名稱</param>
        /// <param name="args">引數</param>
        public void DynamicCmdToClient(string methodName, List<object> args)
        {
            string connId = Context.ConnectionId;
            ClientInfo toClient = CurrClients.First(cc => cc.GUID == connId);
            string cmdCodeStr = RecordMethodQueue(toClient, methodName, args);
            Clients.Client(connId).ExecDynamicCmd(methodName, args, cmdCodeStr);

            SaveStateToXML();
        }

        #endregion

        #endregion

        #region SignalR overridden event

        public override Task OnConnected()
        {
            try {
                // 讀取上次啟動時客戶端的狀態
                if (!CurrClients.Any()) {
                    RestoreFromXML();
                }

                // 取客戶端資訊
                string connId = Context.ConnectionId,
                name = Context.QueryString["Name"],
                vCode = Context.QueryString["ValidECode"],
                jsonGroupNames = Context.QueryString["JsonGroupNames"];

                // 嘗試反序列化從 client 傳來的 json 群組資訊
                List<string> gNames = new List<string>();
                if (!string.IsNullOrEmpty(jsonGroupNames)) {
                    try {
                        gNames = JsonConvert.DeserializeObject<List<string>>(jsonGroupNames);
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Exception capture when deserialize. exMsg: {ex.Message}");
                    }
                }

                // 舊 client 新 instance(GUID改變)，重設客戶端狀態
                ClientInfo oriClient = FindIfClientExist(name, vCode);
                if (oriClient != null) {
                    oriClient.IsConnect = true;
                    CurrClients.RemoveAll(cc => cc.GUID == oriClient.GUID);
                    oriClient.GUID = connId;

                    if (oriClient.GroupNames == null) {
                        oriClient.GroupNames = new List<string>();
                    }
                    gNames.ForEach(gn => {
                        if (!oriClient.GroupNames.Any(oGN => oGN == gn)) {
                            oriClient.GroupNames.Add(gn);
                        }
                    });
                    CurrClients.Add(oriClient);
                }
                else {
                    // 從未加入過的 client
                    CurrClients.Add(new ClientInfo
                    {
                        GUID = connId,
                        Name = name,
                        ValidECode = vCode,
                        GroupNames = gNames,
                        IsConnect = true
                    });
                }

                SaveStateToXML();
                // 重新加回到 SignalR 群組
                CurrClients.ForEach(cc => {
                    cc.GroupNames.ForEach(gn => Groups.Add(cc.GUID, gn));
                });

                // 若是既有客戶端，重送所有佇列中的方法
                if (oriClient != null) {
                    ResendMethodInQueue(oriClient);
                }

                Console.WriteLine($"New connect: {connId}, vCode: {vCode}, name: {name}, gNames: {string.Join(",", CurrClients.FirstOrDefault(cc => cc.GUID == connId).GroupNames)}");
            }
            catch (Exception ex) {
                string exMsg = ex.Message;
            }

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string connId = Context.ConnectionId;
            ClientInfo oriClient = CurrClients.FirstOrDefault(cc => cc.GUID == connId);

            //lock (CurrClients) {
            //if (CurrClients.ContainsKey(connId)) {
            if (oriClient != null) {
                oriClient.IsConnect = false;
            }

            SaveStateToXML();

            Console.WriteLine($"{oriClient.ValidECode}{oriClient.Name} disconnected.");

            stopCalled = true;
            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            try {
                // 讀取上次啟動時客戶端的狀態
                if (!CurrClients.Any()) {
                    RestoreFromXML();
                }

                string connId = Context.ConnectionId;
                ClientInfo oriClient = CurrClients.FirstOrDefault(cc => cc.GUID == connId);
                if (oriClient != null) {
                    oriClient.IsConnect = true;
                }
                else {
                    // 嘗試反序列化從 client 傳來的 json 群組資訊
                    string jsonGroupNames = Context.QueryString["JsonGroupNames"];
                    List<string> gNames = new List<string>();
                    if (!string.IsNullOrEmpty(jsonGroupNames)) {
                        try {
                            gNames = JsonConvert.DeserializeObject<List<string>>(jsonGroupNames);
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Exception capture when deserialize. exMsg: {ex.Message}");
                        }
                    }

                    CurrClients.Add(new ClientInfo
                    {
                        GUID = connId,
                        Name = Context.QueryString["Name"],
                        ValidECode = Context.QueryString["ValidECode"],
                        GroupNames = gNames,
                        IsConnect = true
                    });
                }

                SaveStateToXML();
                // 重新加到 SignalR 群組
                CurrClients.ForEach(cc => cc.GroupNames.ForEach(gn => Groups.Add(cc.GUID, gn)));

                // 若是既有客戶端，重送所有佇列中的方法
                if (oriClient != null) {
                    ResendMethodInQueue(oriClient);
                }

                Console.WriteLine($"{oriClient.ValidECode}{oriClient.Name} reconnected.");
            }
            catch (Exception ex) {
                string exMsg = ex.Message;
            }

            return base.OnReconnected();
        }

        #endregion
    }
}