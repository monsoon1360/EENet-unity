

using System;
using System.IO;

namespace EENet
{
    public interface ITransport : IDisposable
    {

        void InitSocket(string url, int port, Action callback);

        void ReadPacket();

        void WritePacket(Packet p);

        bool  IsReady();

        BinaryWriter  GetWriter();

        BinaryReader  GetReader();

        void OnDisconnect();
        

    }
}