#if !Free
using System;
using System.Text;

namespace VRCAntiTCP.General
{
    public class ByteBuilder : MarshalByRefObject
    {
        internal int Length
        {
            get
            {
                int num = 0;
                for (int i = 0; i < this.used; i++)
                {
                    num += this.data[i].Length;
                }
                return num;
            }
        }

        internal byte this[int i]
        {
            get
            {
                return this.Read(i, 1)[0];
            }
        }

        internal ByteBuilder() : this(10)
        {
        }

        internal ByteBuilder(int packsize)
        {
            this.packsize = packsize;
            this.used = 0;
            this.data = new byte[packsize][];
        }

        internal ByteBuilder(byte[] data)
        {
            this.packsize = 1;
            this.used = 1;
            this.data = new byte[][]
            {
                data
            };
        }

        internal ByteBuilder(byte[] data, int len) : this(data, 0, len)
        {
        }

        internal ByteBuilder(byte[] data, int from, int len) : this(1)
        {
            this.Add(data, from, len);
        }

        internal void Add(byte[] moredata)
        {
            this.Add(moredata, 0, moredata.Length);
        }

        internal void Add(byte[] moredata, int from, int len)
        {
            if (this.used < this.packsize)
            {
                this.data[this.used] = new byte[len];
                for (int i = from; i < from + len; i++)
                {
                    this.data[this.used][i - from] = moredata[i];
                }
                this.used++;
            }
            else
            {
                byte[] array = new byte[this.Length + len];
                int num = 0;
                for (int j = 0; j < this.used; j++)
                {
                    for (int k = 0; k < this.data[j].Length; k++)
                    {
                        array[num++] = this.data[j][k];
                    }
                }
                for (int l = from; l < from + len; l++)
                {
                    array[num++] = moredata[l];
                }
                this.data[0] = array;
                for (int m = 1; m < this.used; m++)
                {
                    this.data[m] = null;
                }
                this.used = 1;
            }
        }

        internal byte[] Read(int from, int len)
        {
            if (len != 0)
            {
                byte[] array = new byte[len];
                int num = 0;
                int num2 = 0;
                for (int i = 0; i < this.used; i++)
                {
                    if (num2 + this.data[i].Length <= from)
                    {
                        num2 += this.data[i].Length;
                    }
                    else
                    {
                        for (int j = 0; j < this.data[i].Length; j++)
                        {
                            if (j + num2 >= from)
                            {
                                array[num++] = this.data[i][j];
                                if (num == len)
                                {
                                    return array;
                                }
                            }
                        }
                    }
                }
                //throw new ArgumentException(string.Concat(new object[]
                //{
                //	"Datapoints ",
                //	from,
                //	" and ",
                //	from + len,
                //	" must be less than ",
                //	this.Length
                //}));
            }
            return new byte[0];
        }

        internal void Clear()
        {
            this.used = 0;
            for (int i = 0; i < this.used; i++)
            {
                this.data[i] = null;
            }
        }

        internal Parameter GetParameter(ref int index)
        {
            Parameter result = default(Parameter);
            result.Type = this.Read(index++, 1)[0];
            byte[] ba = this.Read(index, 4);
            index += 4;
            int @int = ClientInfo.GetInt(ba, 0, 4);
            result.content = this.Read(index, @int);
            index += @int;
            return result;
        }

        internal void AddParameter(Parameter param)
        {
            this.AddParameter(param.content, param.Type);
        }

        internal void AddParameter(byte[] content, byte Type)
        {
            this.Add(new byte[]
            {
                Type
            });
            this.Add(ClientInfo.IntToBytes(content.Length));
            this.Add(content);
        }

        internal static string FormatParameter(Parameter p)
        {
            string result;
            switch (p.Type)
            {
                case 1:
                    {
                        int[] intArray = ClientInfo.GetIntArray(p.content);
                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (int num in intArray)
                        {
                            stringBuilder.Append(num + " ");
                        }
                        result = stringBuilder.ToString();
                        break;
                    }
                case 2:
                    {
                        int[] intArray = ClientInfo.GetIntArray(p.content);
                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (int num2 in intArray)
                        {
                            stringBuilder.Append(num2.ToString("X8") + " ");
                        }
                        result = stringBuilder.ToString();
                        break;
                    }
                case 3:
                    result = Encoding.UTF8.GetString(p.content);
                    break;

                case 4:
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (int num3 in p.content)
                        {
                            stringBuilder.Append(num3.ToString("X2") + " ");
                        }
                        result = stringBuilder.ToString();
                        break;
                    }
                case 5:
                    {
                        string[] stringArray = ClientInfo.GetStringArray(p.content, Encoding.UTF8);
                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (string str in stringArray)
                        {
                            stringBuilder.Append(str + "; ");
                        }
                        result = stringBuilder.ToString();
                        break;
                    }
                default:
                    result = "??";
                    break;
            }
            return result;
        }

        private byte[][] data;

        private int packsize;

        private int used;
    }
}
#endif