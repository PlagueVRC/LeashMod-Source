#if !Free
namespace VRCAntiTCP.General
{
    internal delegate void ConnectionReadBytes(ClientInfo ci, byte[] bytes, int len);
}
#endif