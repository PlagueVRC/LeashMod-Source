#if !Free
using System;

namespace VRCAntiTCP.General
{
    [Serializable]
    internal struct Parameter
    {
        internal Parameter(byte[] content, byte type)
        {
            this.content = content;
            this.Type = type;
        }

        internal byte Type;

        internal byte[] content;
    }
}
#endif