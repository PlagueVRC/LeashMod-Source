#if !Free
using System;

namespace VRCAntiTCP.General
{
    public class ConnectionException : Exception
    {
        internal ConnectionException(string message) : base(message)
        {
        }
    }
}
#endif