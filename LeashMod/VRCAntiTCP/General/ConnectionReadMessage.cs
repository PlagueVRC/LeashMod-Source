#if !Free
namespace VRCAntiTCP.General
{
    internal delegate void ConnectionReadMessage(ClientInfo ci, uint code, byte[] bytes, int len);
}
#endif