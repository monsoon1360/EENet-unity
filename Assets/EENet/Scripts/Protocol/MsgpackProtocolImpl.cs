using System.IO;
using GameDevWare.Serialization;

namespace EENet
{

    public class MsgpackProtocolImpl : IProtocol
    {
        public byte[] Marshal(object msg)
        {
            var stream = new MemoryStream();
            MsgPack.Serialize(msg, stream, SerializationOptions.SuppressTypeInformation);
            byte[] data = stream.ToArray();
            return data;
        }

        public T Unmarshal<T>(byte[] data)
        {
            var stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Position = 0;
            var dic = MsgPack.Deserialize<T>(stream, SerializationOptions.SuppressTypeInformation);
            return dic;
        }
    }
}