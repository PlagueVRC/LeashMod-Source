#if !Free
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using VRC.Core;
using VRCAntiTCP.General;

namespace LeashMod
{
    public static class TCPClient
    {
        internal static ClientInfo client;

        internal static Socket socket;

        internal static bool HasAuthed = false;

        internal static void StartClient()
        {
            try
            {
                if (!File.Exists(Environment.CurrentDirectory + "\\LeashToken.txt"))
                {
                    return;
                }

                socket = Sockets.CreateTCPSocket("VRCAntiCrash.com", 11005);

                client = new ClientInfo(socket, false);

                client.OnRead -= ReadData;
                client.OnReadBytes -= Client_OnReadBytes;
                client.OnReadMessage -= Client_OnReadMessage;

                client.OnRead += ReadData;
                client.OnReadBytes += Client_OnReadBytes;
                client.OnReadMessage += Client_OnReadMessage;

                client.BeginReceive();

                client.Send("<LeashModByPlague_8721>" + APIUser.CurrentUser.id + ":" + File.ReadAllText(Environment.CurrentDirectory + "\\LeashToken.txt"));
            }
            catch
            {
            }
        }

        internal static void ReadData(ClientInfo ci, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (text.Contains("<LeashModResponse_8721>Sucessfully Authed!"))
            {
                HasAuthed = true;

                LeashMod.Log("Authed!");
            }
        }

        internal static void Client_OnReadBytes(ClientInfo ci, byte[] bytes, int len)
        {
            ReadData(ci, Encoding.UTF8.GetString(bytes, 0, len));
        }

        internal static void Client_OnReadMessage(ClientInfo ci, uint code, byte[] bytes, int len)
        {
            ReadData(ci, Encoding.UTF8.GetString(bytes, 0, len));
        }
    }
}
#endif