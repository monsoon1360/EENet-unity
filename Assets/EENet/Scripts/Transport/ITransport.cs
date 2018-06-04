

using System;

namespace EENet
{
    public interface ITransport : IDisposable
    {

        void InitSocket(string url, int port, Action callback);

        Packet ReadPacket();

        void WritePacket(Packet p);

        bool  IsReady();
        

    }
}