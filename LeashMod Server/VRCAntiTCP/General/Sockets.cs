// VRCAntiTCP.General.Sockets
using VRCAntiTCP.General;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace VRCAntiTCP.General
{
    public class Sockets
    {
        public static SocksProxy SocksProxy;

        public static bool UseSocks = false;

        private static string[] errorMsgs = new string[10]
        {
            "Operation completed successfully.",
            "General SOCKS server failure.",
            "Connection not allowed by ruleset.",
            "Network unreachable.",
            "Host unreachable.",
            "Connection refused.",
            "TTL expired.",
            "Command not supported.",
            "Address type not supported.",
            "Unknown error."
        };

        public static Socket CreateTCPSocket(string address, int port)
        {
            return CreateTCPSocket(address, port, UseSocks, SocksProxy);
        }

        public static Socket CreateTCPSocket(string address, int port, bool useSocks, SocksProxy proxy)
        {
            Socket socket;
            if (useSocks)
            {
                socket = ConnectToSocksProxy(proxy.host, proxy.port, address, port, proxy.username, proxy.password);

                return socket;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    var address2 = Dns.GetHostEntry(address).AddressList[0];
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(new IPEndPoint(address2, port));

                    return socket;
                }
                else
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(new IPEndPoint(IPAddress.Parse("94.5.145.0"), port));

                    return socket;
                }
            }

            return null;
        }

        public static Socket ConnectToSocksProxy(IPAddress proxyIP, int proxyPort, string destAddress, int destPort,
            string userName, string password)
        {
            var array = new byte[257];
            var array2 = new byte[257];
            IPAddress iPAddress = null;
            try
            {
                iPAddress = IPAddress.Parse(destAddress);
            }
            catch
            {
            }

            var remoteEP = new IPEndPoint(proxyIP, proxyPort);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(remoteEP);
            ushort size = 0;
            array[size++] = 5;
            array[size++] = 2;
            array[size++] = 0;
            array[size++] = 2;
            socket.Send(array, size, SocketFlags.None);
            var num = socket.Receive(array2, 2, SocketFlags.None);
            if (num != 2)
            {
                //throw new ConnectionException("Bad response received from proxy server.");
            }

            if (array2[1] == byte.MaxValue)
            {
                socket.Close();
                //throw new ConnectionException("None of the authentication method was accepted by proxy server.");
            }

            size = 0;
            array[size++] = 5;
            array[size++] = (byte) userName.Length;
            var bytes = Encoding.Default.GetBytes(userName);
            bytes.CopyTo(array, size);
            size = (ushort) (size + (ushort) bytes.Length);
            array[size++] = (byte) password.Length;
            bytes = Encoding.Default.GetBytes(password);
            bytes.CopyTo(array, size);
            size = (ushort) (size + (ushort) bytes.Length);
            socket.Send(array, size, SocketFlags.None);
            num = socket.Receive(array2, 2, SocketFlags.None);
            if (num != 2)
            {
                //throw new ConnectionException("Bad response received from proxy server.");
            }

            if (array2[1] != 0)
            {
                //throw new ConnectionException("Bad Usernaem/Password.");
            }

            size = 0;
            array[size++] = 5;
            array[size++] = 1;
            array[size++] = 0;
            if (iPAddress != null)
            {
                switch (iPAddress.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        array[size++] = 1;
                        bytes = iPAddress.GetAddressBytes();
                        bytes.CopyTo(array, size);
                        size = (ushort) (size + (ushort) bytes.Length);
                        break;
                    case AddressFamily.InterNetworkV6:
                        array[size++] = 4;
                        bytes = iPAddress.GetAddressBytes();
                        bytes.CopyTo(array, size);
                        size = (ushort) (size + (ushort) bytes.Length);
                        break;
                }
            }
            else
            {
                array[size++] = 3;
                array[size++] = Convert.ToByte(destAddress.Length);
                bytes = Encoding.Default.GetBytes(destAddress);
                bytes.CopyTo(array, size);
                size = (ushort) (size + (ushort) bytes.Length);
            }

            var bytes2 = BitConverter.GetBytes((ushort) destPort);
            for (var num2 = bytes2.Length - 1; num2 >= 0; num2--)
            {
                array[size++] = bytes2[num2];
            }

            socket.Send(array, size, SocketFlags.None);
            socket.Receive(array2);
            if (array2[1] != 0)
            {
                //throw new ConnectionException(errorMsgs[array2[1]]);
            }

            return socket;
        }
    }
}