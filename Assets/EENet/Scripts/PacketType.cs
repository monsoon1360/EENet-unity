namespace EENet
{


    public class PacketType
    {
        public const  byte Req = 0x01;
        public const byte Response = 0x02;
        public const byte Publish = 0x03;
        public const byte Push = 0x04;
        public const byte PingReq = 0x05;
        public const byte PingRes = 0x06;
        public const byte Disconnect = 0x07;
        public const byte Cmd = 0x08;

    }

}