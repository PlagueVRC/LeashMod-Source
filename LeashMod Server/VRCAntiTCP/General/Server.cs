// VRCAntiTCP.General.Server
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LeashMod_Server;

namespace VRCAntiTCP.General
{
    public class Server
    {
        private class ClientState
        {
            internal const int BufferSize = 1024;

            private Socket Socket = null;

            internal byte[] buffer = new byte[1024];

            internal StringBuilder sofar = new StringBuilder();

            internal ClientState(Socket sock)
            {
                Socket = sock;
            }
        }

        private List<ClientInfo> clients = new List<ClientInfo>();

        public IEnumerable Clients => clients;

        public Socket ServerSocket { get; }

        public ClientInfo this[int id]
        {
            get
            {
                foreach (ClientInfo client in Clients)
                {
                    if (client != null)
                    {
                        if (client.ID == id)
                        {
                            return client;
                        }
                    }
                }

                return null;
            }
        }

        public EncryptionType DefaultEncryptionType { get; set; }

        public int Port => ((IPEndPoint)ServerSocket.LocalEndPoint).Port;

        public event ClientEvent Connect;

        public event ClientEvent ClientReady;

        public Server(int port)
            : this(port, null)
        {
        }

        public Server(int port, ClientEvent connDel)
        {
            if (connDel == null)
            {
                return;
            }

            Connect = connDel;
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket?.Bind(new IPEndPoint(IPAddress.Any, port));
            ServerSocket?.Listen(100);
            ServerSocket?.BeginAccept(AcceptCallback, ServerSocket);
        }

        internal void ClientClosed(ClientInfo ci)
        {
            try
            {
                if (ci != null && clients.Contains(ci))
                {
                    clients.Remove(ci);
                }
            }
            catch
            {

            }
        }

        public async Task Broadcast(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var clientsFound = new List<ClientInfo>(clients);

            for (var index = 0; index < clientsFound.Count; index++)
            {
                var client = clientsFound[index];

                try
                {
                    client?.Send(text);
                }
                catch (Exception ex)
                {
                    ServerForm.SendLog("Exception Caught - Carrying On [Broadcast]: " + ex, ServerForm.LogType.Error);

                    continue;
                }

                await Task.Delay(1500);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                if (ar == null)
                {
                    return;
                }

                Socket socket = (Socket)ar.AsyncState;
                Socket socket2 = socket.EndAccept(ar);
                socket.BeginAccept(AcceptCallback, socket);

                ClientInfo clientInfo = new ClientInfo(socket2, null, null, ClientDirection.Both, StartNow: false);

                if (clientInfo == null)
                {
                    return;
                }

                clientInfo.server = this;
                if (Connect != null && !Connect(this, clientInfo))
                {
                    socket2.Close();
                }
                else
                {
                    clientInfo.EncryptionType = DefaultEncryptionType;
                    switch (DefaultEncryptionType)
                    {
                        case EncryptionType.None:
                            KeyExchangeComplete(clientInfo);
                            break;
                        case EncryptionType.ServerKey:
                        {
                            clientInfo.encKey = GetSymmetricKey();
                            byte[] lengthEncodedVector = ClientInfo.GetLengthEncodedVector(clientInfo.encKey);
                            socket2?.Send(lengthEncodedVector);
                            clientInfo.MakeEncoders();
                            KeyExchangeComplete(clientInfo);
                            break;
                        }
                        case EncryptionType.ServerRSAClientKey:
                        {
                            RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
                            RSAParameters encParams =
                                rSACryptoServiceProvider.ExportParameters(includePrivateParameters: true);
                            socket2?.Send(ClientInfo.GetLengthEncodedVector(encParams.Modulus));
                            socket2?.Send(ClientInfo.GetLengthEncodedVector(encParams.Exponent));
                            clientInfo.encParams = encParams;
                            break;
                        }
                    }

                    clients.Add(clientInfo);
                    clientInfo.BeginReceive();
                }
            }
            catch (Exception value)
            {
                ServerForm.SendLog(value.ToString(), ServerForm.LogType.Error);
            }
        }

        protected virtual byte[] GetSymmetricKey()
        {
            return EncryptionUtils.GetRandomBytes(24, addByte: false);
        }

        internal void KeyExchangeComplete(ClientInfo ci)
        {
            if (ClientReady != null && !ClientReady(this, ci))
            {
                ci?.Close();
            }
        }

        ~Server()
        {
            Close();
        }

        public void Close()
        {
            var list = new List<ClientInfo>();

            foreach (ClientInfo client in clients)
            {
                list.Add(client);
            }

            foreach (ClientInfo item in list)
            {
                item.Close();
            }

            try
            {
                ServerSocket?.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                ServerSocket?.Close();
            }
        }
    }
}