using System;
using System.IO;

namespace EENet
{
    public interface IBound : IDisposable
    {

        void Decode();

        byte[] Encode(Packet p);

    }

}
