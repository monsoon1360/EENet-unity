namespace EENet
{

    public enum InboundState
    {
        readHead = 1,
        readLength = 2,
        readBody = 3,
        closed = 4
    }
}