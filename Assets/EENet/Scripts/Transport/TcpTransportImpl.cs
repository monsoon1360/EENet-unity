

using System;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

namespace EENet
{

    public class TcpTransportImpl : ITransport
    {

        private TcpClient tcpClient;

        private NetworkStream stream;

        private BinaryWriter binaryWriter;

        private BinaryReader binaryReader;

        private IAsyncResult  asyncSendCallback;

        private bool isReady;

        private EEClient ee;

        private IBound codec;



        public TcpTransportImpl(EEClient ee, Action<Byte[]> callback)
        {
            this.ee = ee;
            codec = new PacketBoundHandler(this, callback);
        }


        
        public void Dispose()
        {
            if (isReady)
            {
                tcpClient.Close();
                codec.Dispose();
                isReady = false;
            }
        }

        public BinaryReader GetReader()
        {
            return this.binaryReader;
        }

        public BinaryWriter GetWriter()
        {
            return this.binaryWriter;
        }

        public void InitSocket(string url, int port, Action callback)
        {
            tcpClient = new TcpClient();
            tcpClient.ReceiveTimeout = 60;
            tcpClient.SendTimeout = 60;
            ee.NetworkStateChange(NetworkState.CONNECTING);
            tcpClient.BeginConnect(url, port, new AsyncCallback((result) =>
            {
                try
                {
                    tcpClient.EndConnect(result);
                    Debug.Log("connect success.");
                    ee.NetworkStateChange(NetworkState.CONNECTED);
                    stream = tcpClient.GetStream();
                    binaryReader = new BinaryReader(stream);
                    binaryWriter = new BinaryWriter(stream);
                    isReady = true;
                    if (callback != null)
                    {
                        callback();
                    }
                }
                catch (SocketException e)
                {
                    Debug.LogError("connect to server error exception." + e.Message);
                    ee.NetworkStateChange(NetworkState.ERROR);
                    Dispose();
                }

            }), tcpClient);
        }

        public bool IsReady()
        {
           return isReady;
        }

        public void OnDisconnect()
        {
            ee.NetworkStateChange(NetworkState.ERROR);
            Dispose();
        }

        public void ReadPacket()
        {
            this.codec.Decode();
        }

        public void WritePacket(Packet packet)
        {
            if (!isReady)
            {
                Debug.Log("TcpSocket is not ready. can not write packet..");
                return;
            }
            byte[] data = this.codec.Encode(packet);
            this.asyncSendCallback = binaryWriter.BaseStream.BeginWrite(data, 0, data.Length, new AsyncCallback(WritePacketCallback), binaryWriter.BaseStream);
            // binaryWriter.Write(data);
            // binaryWriter.Flush();
        }

        private void WritePacketCallback(IAsyncResult result)
        {
            binaryWriter.BaseStream.EndWrite(result);
        }
    }
}