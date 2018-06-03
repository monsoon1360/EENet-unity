using UnityEngine;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System;
using EENet;
using System.Threading;

namespace EENet
{
    public class TcpSocket : IDisposable
    {

        private TcpClient client;

        private NetworkStream stream;

        private BinaryWriter binaryWriter;

        private BinaryReader binaryReader;

        private bool isReady;

        public TcpSocket()
        {

        }

        public void InitSocket(string url, int port, Action callback)
        {

            client = new TcpClient();
            client.ReceiveTimeout = 60;
            client.SendTimeout = 60;
            client.BeginConnect(url, port, new AsyncCallback((result) =>
            {
                try
                {
                    client.EndConnect(result);
                    if (client.Connected)
                    {
                        Debug.Log("connect success.");
                    }
                    else
                    {
                        Debug.Log("connect failed.");
                    }
                    stream = client.GetStream();
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
                    throw e;
                }

            }), client);
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

        public void Dispose()
        {
            if (isReady)
            {
                client.Close();
                isReady = false;
            }
        }
    }

}
