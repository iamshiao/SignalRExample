using System;
using System.Net;
using System.Net.Sockets;
using CircleHsiao.Extensions;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;

namespace Ptc.iPos.SignalR.Server
{
    /// <summary>SignalRService</summary>
    public class SignalRService
    {
        #region Field & Constructor & Property

        private IHubContext _contxt = null;

        /// <summary>SignalRService</summary>
        public SignalRService(string profileIniPath = null)
        {
            try {
                INI ini;
                if (!string.IsNullOrEmpty(profileIniPath)) {
                    ini = new INI(profileIniPath);
                }
                else {
                    ini = new INI();
                }

                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList) {
                    if (ip.AddressFamily == AddressFamily.InterNetwork) {
                        if (ini.Read("SignalR", "ServerURL") != ip.ToString()) {
                            ini.Write("SignalR", "ServerURL", $"http://{ip.ToString()}:5051");
                        }
                        //URL = $"http://{ip.ToString()}:5051";
                        URL = $"http://127.0.0.1:5051";
                        break;
                    }
                }

                WebApp.Start(URL);
                _contxt = GlobalHost.ConnectionManager.GetHubContext<SignalRServerHub>();
            }
            catch (Exception ex) {
                Console.WriteLine($"啟動 SignalR Server 過程錯誤 {ex.Message}");
                throw;
            }
        }

        /// <summary>Server 所在網址</summary>
        public string URL { get; set; }

        #endregion

        #region Server proactive method

        public void BrocastMsgToAll(string msg, string from)
        {
            // 不支援取目前正在運作的 Hub
            //DefaultHubManager hubManager = new DefaultHubManager(GlobalHost.DependencyResolver);
            //var hub = hubManager.ResolveHub("SignalRServer") as SignalRServer;
            //hub.BrocastMsgToAll("Test", "Admin");

            _contxt.Clients.All.RecievedMsg(msg, from, "cmdCode:Admin");
        }

        #endregion
    }
}