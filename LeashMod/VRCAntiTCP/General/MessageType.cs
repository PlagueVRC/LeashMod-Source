#if !Free
namespace VRCAntiTCP.General
{
    internal enum MessageType
    {
        Unmessaged,
        EndMarker,
        Length,
        CodeAndLength
    }
}
#endif