using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ptc.iPos.SignalR.Client
{
    internal class Program
    {
        private static void Main()
        {
            Console.Write("vCode:");
            var vCode = Console.ReadLine();
            Console.Write("name:");
            var name = Console.ReadLine();
            MsgClient srCli = new MsgClient("http://127.0.0.1:5051", vCode, name);

            while (true) {
                string input = Console.ReadLine();

                if (input.Contains("g:")) {
                    srCli.JoinGroup(input.Replace("g:", ""));
                }
                else if (input.Contains("g-")) {
                    string whole = input.Replace("g-", ""),
                        gName = whole.Split(',')[0],
                        msg = whole.Split(',')[1];

                    List<object> args = new List<object>() { msg };
                    string cmdCode = srCli.RecordMethodQueue("DynamicCmdToGroup", args);
                    srCli.HubProxy.Invoke("DynamicCmdToGroup", "Msg", args, gName, cmdCode, false);
                }
                else if (input.Contains("l:")) {
                    srCli.LeaveGroup(input.Replace("l:", ""));
                }
                else if (input == "X") {
                    srCli.SelfDisconnect();
                }
                else if (input == "C") {
                    srCli.Connect();
                }
                else if (input == "list") {
                    List<string> list = new List<string>() { "A", "B", "C" };
                    string json = JsonConvert.SerializeObject(list);
                    List<object> args = new List<object>() { json, "D" };
                    string cmdCode = srCli.RecordMethodQueue("ListAndMsg", args, "DynamicCmdToAll");
                    srCli.HubProxy.Invoke("DynamicCmdToAll", "ListAndMsg", args, cmdCode, false);
                }
                else if (input == "from") {
                    List<object> args = new List<object>() { srCli.Name, input };
                    string cmdCode = srCli.RecordMethodQueue("NamedMsg", args, "DynamicCmdToAll");
                    srCli.HubProxy.Invoke("DynamicCmdToAll", "NamedMsg", args, cmdCode, false);
                }
                else if (input == "json") {
                    List<object> args = new List<object>() { "[{\"TabelName\":\"SF_StoreStaffDetail\",\"GenerateAt\":\"2016-11-18T00:00:00\",\"Seq\":1,\"FullPath\":\"C:\\SPCC_SC\\Receive\\MDAD\\SF_StoreStaffDetail_201611180001.json\"},{\"TabelName\":\"ST_StoreArea\",\"GenerateAt\":\"2016-11-18T00:00:00\",\"Seq\":1,\"FullPath\":\"C:\\SPCC_SC\\Receive\\MDAD\\ST_StoreArea_201611180001.json\"}]" };
                    string cmdCode = srCli.RecordMethodQueue("NamedMsg", args, "DynamicCmdToAll");
                    srCli.HubProxy.Invoke("DynamicCmdToAll", "NamedMsg", args, cmdCode, false);
                }
                else {
                    List<object> args = new List<object>() { input };
                    string cmdCode = srCli.RecordMethodQueue("Msg", args, "DynamicCmdToAll");
                    srCli.HubProxy.Invoke("DynamicCmdToAll", "Msg", args, cmdCode, false);
                }
            }
        }
    }
}