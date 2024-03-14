// VRCAntiTCP.General.ClientInfo
using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using LeashMod_Server;
using VRCAntiTCP.Cryptography;
using VRCAntiTCP.General;

public class ClientInfo
{
    public const int BUFSIZE = 1024;

    internal Server server = null;

    private Socket sock;

    private string buffer;

    public MessageType MessageType;

    private ClientDirection dir;

    private int id;

    private bool alreadyclosed = false;

    public static int NextID = 100;

    public object Data = null;

    private EncryptionType encType;

    private int encRead = 0;

    private int encStage;

    private int encExpected;

    internal bool encComplete;

    internal byte[] encKey;

    internal RSAParameters encParams;

    internal ICryptoTransform encryptor;

    internal ICryptoTransform decryptor;

    private string delim;

    private byte[] buf = new byte[1024];

    private ByteBuilder bytes = new ByteBuilder(10);

    private byte[] msgheader = new byte[8];

    private byte headerread = 0;

    private bool wantingChecksum = true;

    public EncryptionType EncryptionType
    {
        get
        {
            return encType;
        }
        set
        {
            if (encStage != 0)
            {
                ////throw new ArgumentException("Key exchange has already begun");
            }
            encType = value;
            encComplete = (encType == EncryptionType.None);
            encExpected = -1;
        }
    }

    public bool EncryptionReady => encComplete;

    public ICryptoTransform Encryptor => encryptor;

    public ICryptoTransform Decryptor => decryptor;

    public string Delimiter
    {
        get
        {
            return delim;
        }
        set
        {
            delim = value;
        }
    }

    public ClientDirection Direction => dir;

    public Socket Socket => sock;

    public Server Server => server;

    public int ID => id;

    public bool Closed => !sock.Connected;

    public event ConnectionRead OnRead;

    public event ConnectionClosed OnClose;

    public event ConnectionReadBytes OnReadBytes;

    public event ConnectionReadMessage OnReadMessage;

    public event ConnectionReadPartialMessage OnPartialMessage;

    public ClientInfo(Socket cl, bool StartNow)
        : this(cl, null, null, ClientDirection.Both, StartNow, EncryptionType.None)
    {
    }

    public ClientInfo(Socket cl, ConnectionRead read, ConnectionReadBytes readevt, ClientDirection d, bool StartNow)
        : this(cl, read, readevt, d, StartNow, EncryptionType.None)
    {
    }

    public ClientInfo(Socket cl, ConnectionRead read, ConnectionReadBytes readevt, ClientDirection d, bool StartNow, EncryptionType encryptionType)
    {
        if (cl == null)
        {
            return;
        }

        sock = cl;
        buffer = "";
        OnReadBytes = readevt;
        encType = encryptionType;
        encStage = 0;
        encComplete = (encType == EncryptionType.None);
        OnRead = read;
        MessageType = MessageType.EndMarker;
        dir = d;
        delim = "\n";
        id = NextID;
        NextID++;

        if (StartNow)
        {
            BeginReceive();
        }
    }

    public void BeginReceive()
    {
        sock.BeginReceive(buf, 0, 1024, SocketFlags.None, ReadCallback, this);
    }

    public string Send(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        byte[] array = Encoding.UTF8.GetBytes(text);
        string text2 = "";
        for (int i = 0; i < array.Length; i++)
        {
            text2 = text2 + array[i] + " ";
        }
        Send(array);
        return text2;
    }

    public void SendMessage(uint code, byte[] bytes)
    {
        if (bytes == null)
        {
            return;
        }

        SendMessage(code, bytes, 0, bytes.Length);
    }

    public void SendMessage(uint code, byte[] bytes, byte paramType)
    {
        if (bytes == null)
        {
            return;
        }

        SendMessage(code, bytes, paramType, bytes.Length);
    }

    public void SendMessage(uint code, byte[] bytes, byte paramType, int len)
    {
        if (bytes == null || sock == null)
        {
            return;
        }

        if (paramType > 0)
        {
            ByteBuilder byteBuilder = new ByteBuilder(3);
            byteBuilder.AddParameter(bytes, paramType);
            bytes = byteBuilder.Read(0, byteBuilder.Length);
            len = bytes.Length;
        }
        lock (sock)
        {
            byte b = 0;
            switch (MessageType)
            {
                case MessageType.CodeAndLength:
                {
                    byte[] array;
                    Send(array = UintToBytes(code));
                    for (int j = 0; j < 4; j++)
                    {
                        b = (byte)(b + array[j]);
                    }
                    Send(array = IntToBytes(len));
                    for (int k = 0; k < 4; k++)
                    {
                        b = (byte)(b + array[k]);
                    }
                    if (encType != 0)
                    {
                        Send(new byte[1]
                        {
                        b
                        });
                    }
                    break;
                }
                case MessageType.Length:
                {
                    byte[] array;
                    Send(array = IntToBytes(len));
                    for (int i = 0; i < 4; i++)
                    {
                        b = (byte)(b + array[i]);
                    }
                    if (encType != 0)
                    {
                        Send(new byte[1]
                        {
                        b
                        });
                    }
                    break;
                }
            }
            Send(bytes, len);
            if (encType != 0)
            {
                b = 0;
                for (int l = 0; l < len; l++)
                {
                    b = (byte)(b + bytes[l]);
                }
                Send(new byte[1]
                {
                    b
                });
            }
        }
    }

    public void Send(byte[] bytes)
    {
        if (bytes == null)
        {
            return;
        }

        Send(bytes, bytes.Length);
    }

    public void Send(byte[] bytes, int len)
    {
        if (bytes == null)
        {
            return;
        }

        if (encType != 0)
        {
            byte[] array = new byte[len];
            Encryptor.TransformBlock(bytes, 0, len, array, 0);
            bytes = array;
        }

        sock.Send(bytes);
    }

    public bool MessageWaiting()
    {
        FillBuffer(sock);
        return buffer.IndexOf(delim) >= 0;
    }

    public string Read()
    {
        int num = buffer.IndexOf(delim);
        if (num >= 0)
        {
            string result = buffer.Substring(0, num);
            buffer = buffer.Substring(num + delim.Length);
            return result;
        }
        return "";
    }

    private void FillBuffer(Socket sock)
    {
        if (sock == null)
        {
            return;
        }

        byte[] array = new byte[256];

        while (sock.Available != 0)
        {
            int num = sock.Receive(array);
            if (OnReadBytes != null)
            {
                OnReadBytes(this, array, num);
            }
            buffer += Encoding.UTF8.GetString(array, 0, num);
        }
    }

    private void ReadCallback(IAsyncResult ar)
    {
        try
        {
            if (ar == null)
            {
                return;
            }

            int num = sock.EndReceive(ar);

            if (num > 0 && buf != null)
            {
                DoRead(buf, num);
                BeginReceive();
            }
            else
            {
                Close();
            }
        }
        catch (SocketException)
        {
            Close();
        }
        catch (ObjectDisposedException)
        {
            Close();
        }
    }

    internal void DoRead(byte[] buf, int read)
    {
        if (read > 0 && OnRead != null)
        {
            buffer += Encoding.UTF8.GetString(buf, 0, read);
            while (buffer.IndexOf(delim) >= 0)
            {
                OnRead(this, Read());
            }
        }
        ReadInternal(buf, read, alreadyEncrypted: false);
    }

    public static void LogBytes(byte[] buf, int len)
    {
        byte[] array = new byte[len];
        Array.Copy(buf, array, len);
        ServerForm.SendLog(ByteBuilder.FormatParameter(new Parameter(array, 4)));
    }

    private void ReadInternal(byte[] buf, int read, bool alreadyEncrypted)
    {
        if (!alreadyEncrypted && encType != 0)
        {
            if (!encComplete)
            {
                int num = 0;
                if (encExpected < 0)
                {
                    encStage++;
                    num++;
                    read--;
                    encExpected = buf[0];
                    encKey = new byte[encExpected];
                    encRead = 0;
                }
                if (read >= encExpected)
                {
                    Array.Copy(buf, num, encKey, encRead, encExpected);
                    int num2 = read - encExpected;
                    encExpected = -1;
                    if (server == null)
                    {
                        ClientEncryptionTransferComplete();
                    }
                    else
                    {
                        ServerEncryptionTransferComplete();
                    }
                    if (num2 > 0)
                    {
                        byte[] destinationArray = new byte[num2];
                        Array.Copy(buf, read + num - num2, destinationArray, 0, num2);
                        ReadInternal(destinationArray, num2, alreadyEncrypted: false);
                    }
                }
                else
                {
                    Array.Copy(buf, num, encKey, encRead, read);
                    encExpected -= read;
                    encRead += read;
                }
                return;
            }
            buf = decryptor.TransformFinalBlock(buf, 0, read);
        }
        if (!alreadyEncrypted && OnReadBytes != null)
        {
            OnReadBytes(this, buf, read);
        }
        if (OnReadMessage == null || MessageType == MessageType.Unmessaged)
        {
            return;
        }
        uint code = 0u;
        switch (MessageType)
        {
            case MessageType.Length:
            case MessageType.CodeAndLength:
            {
                int num3;
                int num4;
                if (MessageType == MessageType.Length)
                {
                    num3 = FillHeader(ref buf, 4, read);
                    if (headerread < 4)
                    {
                        break;
                    }
                    num4 = GetInt(msgheader, 0, 4);
                }
                else
                {
                    num3 = FillHeader(ref buf, 8, read);
                    if (headerread < 8)
                    {
                        break;
                    }
                    code = (uint)GetInt(msgheader, 0, 4);
                    num4 = GetInt(msgheader, 4, 4);
                }
                if (read == num3)
                {
                    break;
                }
                int num5 = 0;
                if (wantingChecksum && encType != 0)
                {
                    byte b = buf[0];
                    num5++;
                    wantingChecksum = false;
                    byte b2 = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        b2 = (byte)(b2 + msgheader[i]);
                    }
                    if (b != b2)
                    {
                        Close();

                    }
                }
                bytes.Add(buf, num5, read - num5 - num3);
                if (encType != 0)
                {
                    num4++;
                }
                if (OnPartialMessage != null)
                {
                    OnPartialMessage(this, code, buf, num5, read - num5 - num3, bytes.Length, num4);
                }
                if (bytes.Length < num4)
                {
                    break;
                }
                headerread = 0;
                wantingChecksum = true;
                byte[] array = bytes.Read(0, num4);
                if (encType != 0)
                {
                    byte b3 = array[num4 - 1];
                    byte b4 = 0;
                    for (int j = 0; j < num4 - 1; j++)
                    {
                        b4 = (byte)(b4 + array[j]);
                    }
                    if (b3 != b4)
                    {
                        Close();

                    }
                    OnReadMessage(this, code, array, num4 - 1);
                }
                else
                {
                    OnReadMessage(this, code, array, num4);
                }
                int num6 = bytes.Length - num4;
                if (num6 > 0)
                {
                    byte[] array2 = bytes.Read(num4, num6);
                    bytes.Clear();
                    ReadInternal(array2, array2.Length, alreadyEncrypted: true);
                }
                else
                {
                    bytes.Clear();
                }
                break;
            }
        }
    }

    private int FillHeader(ref byte[] buf, int to, int read)
    {
        int num = 0;
        if (headerread < to)
        {
            int num2 = 0;
            while (num2 < read && headerread < to)
            {
                msgheader[headerread] = buf[num2];
                num2++;
                headerread++;
                num++;
            }
        }
        if (num > 0)
        {
            byte[] array = new byte[read - num];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = buf[i + num];
            }
            buf = array;
        }
        return num;
    }

    internal ICryptoTransform MakeEncryptor()
    {
        return MakeCrypto(encrypt: true);
    }

    internal ICryptoTransform MakeDecryptor()
    {
        return MakeCrypto(encrypt: false);
    }

    internal ICryptoTransform MakeCrypto(bool encrypt)
    {
        if (encrypt)
        {
            return new SimpleEncryptor(encKey);
        }
        return new SimpleDecryptor(encKey);
    }

    private void ServerEncryptionTransferComplete()
    {
        switch (encType)
        {
            case EncryptionType.ServerRSAClientKey:
            {
                RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
                rSACryptoServiceProvider.ImportParameters(encParams);
                encKey = rSACryptoServiceProvider.Decrypt(encKey, fOAEP: false);
                MakeEncoders();
                server.KeyExchangeComplete(this);
                break;
            }
        }
    }

    private void ClientEncryptionTransferComplete()
    {
        switch (encType)
        {
            case EncryptionType.ServerKey:
                MakeEncoders();
                break;
            case EncryptionType.ServerRSAClientKey:
                switch (encStage)
                {
                    case 1:
                        encParams.Modulus = encKey;
                        break;
                    case 2:
                    {
                        encParams.Exponent = encKey;
                        RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
                        rSACryptoServiceProvider.ImportParameters(encParams);
                        encKey = EncryptionUtils.GetRandomBytes(24, addByte: false);
                        byte[] lengthEncodedVector = GetLengthEncodedVector(rSACryptoServiceProvider.Encrypt(encKey, fOAEP: false));
                        sock.Send(lengthEncodedVector);
                        MakeEncoders();
                        break;
                    }
                }
                break;
        }
    }

    internal void MakeEncoders()
    {
        encryptor = MakeEncryptor();
        decryptor = MakeDecryptor();
        encComplete = true;
    }

    public static byte[] GetLengthEncodedVector(byte[] from)
    {
        int num = from.Length;
        if (num > 255)
        {
            ////throw new ArgumentException("Cannot length encode more than 255");
        }
        byte[] array = new byte[num + 1];
        array[0] = (byte)num;
        Array.Copy(from, 0, array, 1, num);
        return array;
    }

    public static int GetInt(byte[] ba, int from, int len)
    {
        int num = 0;
        for (int i = 0; i < len; i++)
        {
            num += ba[from + i] << (len - i - 1) * 8;
        }
        return num;
    }

    public static int[] GetIntArray(byte[] ba)
    {
        return GetIntArray(ba, 0, ba.Length);
    }

    public static int[] GetIntArray(byte[] ba, int from, int len)
    {
        int[] array = new int[len / 4];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = GetInt(ba, from + i * 4, 4);
        }
        return array;
    }

    public static uint[] GetUintArray(byte[] ba)
    {
        uint[] array = new uint[ba.Length / 4];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = (uint)GetInt(ba, i * 4, 4);
        }
        return array;
    }

    public static byte[] IntToBytes(int val)
    {
        return UintToBytes((uint)val);
    }

    public static byte[] UintToBytes(uint val)
    {
        byte[] array = new byte[4];
        for (int num = 3; num >= 0; num--)
        {
            array[num] = (byte)val;
            val >>= 8;
        }
        return array;
    }

    public static byte[] IntArrayToBytes(int[] val)
    {
        byte[] array = new byte[val.Length * 4];
        for (int i = 0; i < val.Length; i++)
        {
            byte[] array2 = IntToBytes(val[i]);
            array[i * 4] = array2[0];
            array[i * 4 + 1] = array2[1];
            array[i * 4 + 2] = array2[2];
            array[i * 4 + 3] = array2[3];
        }
        return array;
    }

    public static byte[] UintArrayToBytes(uint[] val)
    {
        byte[] array = new byte[val.Length * 4];
        for (uint num = 0u; num < val.Length; num++)
        {
            byte[] array2 = IntToBytes((int)val[num]);
            array[num * 4] = array2[0];
            array[num * 4 + 1] = array2[1];
            array[num * 4 + 2] = array2[2];
            array[num * 4 + 3] = array2[3];
        }
        return array;
    }

    public static byte[] StringArrayToBytes(string[] val, Encoding e)
    {
        byte[][] array = new byte[val.Length][];
        int num = 0;
        for (int i = 0; i < val.Length; i++)
        {
            array[i] = e.GetBytes(val[i]);
            num += 4 + array[i].Length;
        }
        byte[] array2 = new byte[num + 4];
        IntToBytes(val.Length).CopyTo(array2, 0);
        int num2 = 4;
        for (int j = 0; j < array.Length; j++)
        {
            IntToBytes(array[j].Length).CopyTo(array2, num2);
            num2 += 4;
            array[j].CopyTo(array2, num2);
            num2 += array[j].Length;
        }
        return array2;
    }

    public static string[] GetStringArray(byte[] ba, Encoding e)
    {
        int @int = GetInt(ba, 0, 4);
        int num = 4;
        string[] array = new string[@int];
        for (int i = 0; i < @int; i++)
        {
            int int2 = GetInt(ba, num, 4);
            num += 4;
            array[i] = e.GetString(ba, num, int2);
            num += int2;
        }
        return array;
    }

    public void Close(bool Force = false)
    {
        #region Call Events
        if (!alreadyclosed)
        {
            if (server != null)
            {
                server.ClientClosed(this);
            }
            if (OnClose != null)
            {
                OnClose(this);
            }
            alreadyclosed = true;
        }
        #endregion

        try
        {
            if (Force)
            {
                sock.Shutdown(SocketShutdown.Both);
            }
            else
            {
                sock.Close();
            }
        }
        catch
        {
            
        }
    }
}
