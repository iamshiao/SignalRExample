using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Ptc.iPos.SignalR.Server.Startup))]

namespace Ptc.iPos.SignalR.Server
{
    /// <summary>Startup</summary>
    public class Startup
    {
        /// <summary>Configuration</summary>
        /// <param name="app">IAppBuilder</param>
        public void Configuration(IAppBuilder app)
        {
            // Make long polling connections wait a maximum of 110 seconds for a
            // response. When that time expires, trigger a timeout command and
            // make the client reconnect.
            // 超過此時間沒有任何 heart beat 回來則進入重連嘗試
            GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(60);

            // Wait a maximum of 30 seconds after a transport connection is lost
            // before raising the Disconnected event to terminate the SignalR connection.
            // 斷線後重連嘗試的總時間，超過此時間會放棄嘗試
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(300);

            // For transports other than long polling, send a keepalive packet every
            // 10 seconds.
            // This value must be no more than 1/3 of the DisconnectTimeout value.
            // 每次 heart beat 的間隔
            GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(10);

            var hubConfiguration = new HubConfiguration
            {
#if DEBUG
                EnableDetailedErrors = true
#else
            EnableDetailedErrors = false
#endif
            };

            //app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
}