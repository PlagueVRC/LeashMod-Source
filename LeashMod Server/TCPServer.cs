using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using VRCAntiTCP.General;

namespace LeashMod_Server
{
    public class TCPServer
    {
        public static Server server;

        public static bool IsRunning = false;

        public class AllTimeUsers
        {
            public List<string> UserList = new List<string>();

        }

        public static AllTimeUsers UsersConfig = new AllTimeUsers();

        public static void StartServer()
        {
            if (!IsRunning)
            {
                try
                {
                    ServerForm.SendLog("Starting..");

                    JsonConfig.LoadConfig(ref UsersConfig);

                    server = new Server(11008, ClientConnect);

                    ServerForm.SendLog("Started!");

                    IsRunning = true;
                }
                catch (Exception ex)
                {
                    ServerForm.SendLog("Exception Caught [StartServer]: " + ex.Message, ServerForm.LogType.Error);
                }
            }
        }

        public static bool ClientConnect(Server serv, ClientInfo new_client)
        {
            if (serv == null || new_client == null)
            {
                ServerForm.SendLog("A NullReferenceException Was Prevented In ClientConnect!", ServerForm.LogType.Error);
                return false;
            }

            try
            {
                //Un-Assign Possibly Previously Assigned Events
                new_client.OnRead -= ReadData;
                new_client.OnReadBytes -= New_client_OnReadBytes;
                new_client.OnReadMessage -= New_client_OnReadMessage;

                new_client.OnRead += ReadData;
                new_client.OnReadBytes += New_client_OnReadBytes;
                new_client.OnReadMessage += New_client_OnReadMessage;

                new_client.BeginReceive();

                return true;
            }
            catch (Exception ex)
            {
                ServerForm.SendLog("Exception Caught [ClientConnect]: " + ex.Message, ServerForm.LogType.Error);
                return false;
            }
        }

        public static Dictionary<string, int> UserStates = new Dictionary<string, int>();

        public static Dictionary<string, ClientInfo> UserDict = new Dictionary<string, ClientInfo>(); // usr_blabla, ClientInfo

        public static async Task ReadData(ClientInfo ci, string text)
        {
            try
            {
                if (ci == null || string.IsNullOrEmpty(text))
                {
                    return;
                }

                var EndpointAddress = ((IPEndPoint)ci.Socket.RemoteEndPoint).Address.ToString();

                var CiInfo = UserDict.FirstOrDefault(o => o.Value == ci);

                void BanUser()
                {
                    if (CiInfo.Value != null)
                    {
                        UserDict.Remove(CiInfo.Key);
                    }

                    ci.Close(true);
                    FirewallAPIHelper.ApplyFirewallRule(EndpointAddress, true);
                }

                ServerForm.SendLog("Received Data From: " + ci.ID + " - " + EndpointAddress + " - Data: " + text);

                var MethodName = text.Substring(0, text.IndexOf("("));
                var Arguments = text.Substring(text.IndexOf("(") + 1, text.IndexOf(")") - (text.IndexOf("(") + 1)).Replace(" ", "").Split(',');

                if ((!MethodName.EndsWith("Data") && Arguments.Length > 0) || (MethodName.EndsWith("Data") && Arguments.Length <= 0)) // Likely Someone Reverse Engineering
                {
                    BanUser();
                    return;
                }

                switch (MethodName)
                {
                    case "Login_Init":
                        UserStates[EndpointAddress] = 0; // Init Stage

                        ci.Send("Init");

                        break;
                    case "Login_Data": // Arg Count: 1 | Arg Data: usr_id:token
                        var UserID = Arguments[0];
                        var TokenUserID = Arguments[1];
                        var Token = Arguments[2];

                        if (!UserStates.ContainsKey(EndpointAddress) || UserStates[EndpointAddress] != 0)
                        {
                            BanUser();
                            return;
                        }

                        if ((await FileUtils.SafelyReadAllText("C:\\Users\\Administrator\\Desktop\\LeashMod\\LeashModTokenList.txt")).Replace("\r", "").Split('\n').Contains(TokenUserID + ":" + Token))
                        {
                            UserStates[EndpointAddress] = 1; // Data Stage, Verified
                            UserDict[UserID] = ci;
                        }

                        break;
                    case "Login_Finish":
                        if (!UserStates.ContainsKey(EndpointAddress) || UserStates[EndpointAddress] != 1 || CiInfo.Value == null)
                        {
                            BanUser();
                            return;
                        }

                        UserStates[EndpointAddress] = 2; // Finish Stage, Connected

                        break;
                    case "WorldChange":
                        if (!UserStates.ContainsKey(EndpointAddress) || UserStates[EndpointAddress] != 2 || CiInfo.Value == null)
                        {
                            BanUser();
                            return;
                        }

                        // WorldChange(usr_blabla, instanceidwithnonce)
                        var TargetUserID = Arguments.FirstOrDefault();
                        var TargetWorld = Arguments.LastOrDefault();

                        var TargetUser = UserDict.FirstOrDefault(o => o.Key == TargetUserID);

                        if (TargetUser.Value != null)
                        {
                            TargetUser.Value.Send($"JoinMaster({CiInfo.Key}, {TargetWorld})");
                        }
                        else
                        {
                            BanUser(); // User Wasn't Logged In That This Was Towards, Assume Worst-Case -> Abuse!
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                ServerForm.SendLog("Exception Caught [ReadData]: " + ex.Message, ServerForm.LogType.Error);
                // Ignore the exception and move the fuck on 4hed
            }
        }

        public static async Task New_client_OnReadBytes(ClientInfo ci, byte[] bytes, int len)
        {
            try
            {
                if (ci == null || bytes == null || bytes.Length == 0)
                {
                    return;
                }

                await ReadData(ci, Encoding.UTF8.GetString(bytes, 0, len));
            }
            catch (Exception ex)
            {
                ServerForm.SendLog("Exception Caught [New_client_OnReadBytes]: " + ex.Message, ServerForm.LogType.Error);
            }
        }

        public static async Task New_client_OnReadMessage(ClientInfo ci, uint code, byte[] bytes, int len)
        {
            try
            {
                if (ci == null || bytes == null || bytes.Length == 0)
                {
                    return;
                }

                await ReadData(ci, Encoding.UTF8.GetString(bytes, 0, len));
            }
            catch (Exception ex)
            {
                ServerForm.SendLog("Exception Caught [New_client_OnReadMessage]: " + ex.Message, ServerForm.LogType.Error);
            }
        }
    }

    public static class Extensions
    {
        internal static bool IsConnected(this Socket socket)
        {
            try
            {
                socket.Send("KeepAlive");

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void Send(this Socket socket, string text)
        {
            var array = Encoding.UTF8.GetBytes(text);
            var text2 = "";
            for (var i = 0; i < array.Length; i++)
            {
                text2 = text2 + array[i] + " ";
            }

            socket.Send(array);
        }
    }
}