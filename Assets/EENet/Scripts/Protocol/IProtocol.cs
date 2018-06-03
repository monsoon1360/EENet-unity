namespace EENet
{
    public interface IProtocol
    {
        byte[] Marshal(object msg);

        T  Unmarshal<T>(byte[] data);
    }


}