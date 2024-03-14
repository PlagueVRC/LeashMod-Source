#if !Free
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace VRCAntiTCP.General
{
    public class Server
    {
        private class ClientState
        {
            internal const int BufferSize = 1024;

            internal Socket Socket = null;

            internal byte[] buffer = new byte[1024];

            internal StringBuilder sofar = new StringBuilder();

            internal ClientState(Socket sock)
            {
                Socket = sock;
            }
        }

        private ArrayList clients = new ArrayList();

        private Socket ss;

        private EncryptionType encType;

        internal IEnumerable Clients => clients;

        internal Socket ServerSocket => ss;

        internal ClientInfo this[int id]
        {
            get
            {
                foreach (ClientInfo client in Clients)
                {
                    if (client.ID == id)
                    {
                        return client;
                    }
                }

                return null;
            }
        }

        internal EncryptionType DefaultEncryptionType
        {
            get { return encType; }
            set { encType = value; }
        }

        internal int Port => ((IPEndPoint)ss.LocalEndPoint).Port;

        internal event ClientEvent Connect;

        internal event ClientEvent ClientReady;

        internal Server(int port)
            : this(port, null)
        {
        }

        internal Server(int port, ClientEvent connDel)
        {
            this.Connect = connDel;
            ss = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ss.Bind(new IPEndPoint(IPAddress.Any, port));
            ss.Listen(100);
            ss.BeginAccept(AcceptCallback, ss);
        }

        internal void ClientClosed(ClientInfo ci)
        {
            clients.Remove(ci);
        }

        internal void Broadcast(byte[] bytes)
        {
            foreach (ClientInfo client in clients)
            {
                client.Send(bytes);
            }
        }

        internal void BroadcastMessage(uint code, byte[] bytes)
        {
            BroadcastMessage(code, bytes, 0);
        }

        internal void BroadcastMessage(uint code, byte[] bytes, byte paramType)
        {
            foreach (ClientInfo client in clients)
            {
                client.SendMessage(code, bytes, paramType);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                Socket socket2 = socket.EndAccept(ar);
                socket.BeginAccept(AcceptCallback, socket);
                ClientInfo clientInfo = new ClientInfo(socket2, null, null, ClientDirection.Both, StartNow: false);
                clientInfo.server = this;
                if (this.Connect != null && !this.Connect(this, clientInfo))
                {
                    socket2.Close();
                }
                else
                {
                    clientInfo.EncryptionType = encType;
                    switch (encType)
                    {
                        case EncryptionType.None:
                            KeyExchangeComplete(clientInfo);
                            break;

                        case EncryptionType.ServerKey:
                            {
                                clientInfo.encKey = GetSymmetricKey();
                                byte[] lengthEncodedVector = ClientInfo.GetLengthEncodedVector(clientInfo.encKey);
                                socket2.Send(lengthEncodedVector);
                                clientInfo.MakeEncoders();
                                KeyExchangeComplete(clientInfo);
                                break;
                            }
                        case EncryptionType.ServerRSAClientKey:
                            {
                                RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
                                RSAParameters encParams =
                                    rSACryptoServiceProvider.ExportParameters(includePrivateParameters: true);
                                socket2.Send(ClientInfo.GetLengthEncodedVector(encParams.Modulus));
                                socket2.Send(ClientInfo.GetLengthEncodedVector(encParams.Exponent));
                                clientInfo.encParams = encParams;
                                break;
                            }
                    }

                    clients.Add(clientInfo);
                    clientInfo.BeginReceive();
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
            catch (Exception value)
            {
                Console.WriteLine(value);
            }
        }

        protected virtual byte[] GetSymmetricKey()
        {
            return EncryptionUtils.GetRandomBytes(24, addByte: false);
        }

        internal void KeyExchangeComplete(ClientInfo ci)
        {
            if (this.ClientReady != null && !this.ClientReady(this, ci))
            {
                ci.Close();
            }
        }

        ~Server()
        {
            Close();
        }

        internal void Close()
        {
            ArrayList arrayList = new ArrayList();
            foreach (ClientInfo client in clients)
            {
                arrayList.Add(client);
            }

            foreach (ClientInfo item in arrayList)
            {
                item.Close();
            }

            ss.Close();
        }
    }
}
#endif