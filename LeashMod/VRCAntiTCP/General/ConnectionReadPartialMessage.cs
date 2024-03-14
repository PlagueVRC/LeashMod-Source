#if !Free
namespace VRCAntiTCP.General
{
    internal delegate void ConnectionReadPartialMessage(ClientInfo ci, uint code, byte[] bytes, int start, int read, int sofar, int totallength);
}
#endif