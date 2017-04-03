using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ptc.iPos.SignalR.Client
{
    internal class MsgClient : SignalRClient
    {
        public MsgClient(string ip, string vCode, string name) : base(ip, "SignalRServerHub", vCode, name)
        {
            base.Connect();
        }

        public void Msg(string msg)
        {
            Console.WriteLine($"文字訊息: {msg}");
        }

        public void NamedMsg(string name, string msg)
        {
            Console.WriteLine($"來自: {name} 的文字訊息: {msg}");
        }

        public void ListAndMsg(string jsonOfList, string msg)
        {
            List<string> list = JsonConvert.DeserializeObject<List<string>>(jsonOfList);
            string joined = string.Join(",", list);
            Console.WriteLine($"文字訊息: {msg}，清單: {joined}");
        }
    }
}
