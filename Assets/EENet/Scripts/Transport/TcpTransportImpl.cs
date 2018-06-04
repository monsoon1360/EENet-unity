

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

        private bool isReady;

        private EEClient ee;

        public TcpTransportImpl(EEClient ee)
        {
            this.ee = ee;
        }


        
        public void Dispose()
        {
            if (isReady)
            {
                tcpClient.Close();
                isReady = false;
            }
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

        public Packet ReadPacket()
        {
            if (!isReady)
            {
                Debug.Log("TcpSocket is not ready. can not read packet..");
                return null;
            }
            Packet p = Packet.ReadFromBinaryReader(this.binaryReader);
            return p;
        }

        public void WritePacket(Packet packet)
        {
            if (!isReady)
            {
                Debug.Log("TcpSocket is not ready. can not write packet..");
                return;
            }
            byte[] data = packet.ToBytes();
            binaryWriter.Write(data);
            binaryWriter.Flush();
        }
    }
}