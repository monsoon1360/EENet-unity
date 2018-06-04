

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using UnityEngine;

namespace EENet
{

    public enum NetworkState
    {
        [Description("init state")]
        CLOSED,

        [Description("connecting server")]
        CONNECTING,

        [Description("server connected")]
        CONNECTED,

        [Description("disconnected with server")]
        DISCONNECTED,

        [Description("connect timeout")]
        TIMEOUT,

        [Description("netwrok error")]
        ERROR
    }

    public class EEClient : IDisposable
    {

        public event Action<NetworkState> NetworkStateChangedEvent;

        private EventManager eventMgr;

        private ITransport transport;

        private IProtocol protocol;

        private UInt32 reqId = 1;

        private NetworkState currNetworkState = NetworkState.CLOSED;

        

        public EEClient()
        {
            
        }

        public void InitClient(String host, int port, Action callback = null)
        {
            transport = new TcpTransportImpl(this);
            protocol = new MsgpackProtocolImpl();
            eventMgr = new EventManager(protocol);
             transport.InitSocket(host, port, callback);
        }

        public void NetworkStateChange(NetworkState newState)
        {
            currNetworkState = newState;
            Debug.Log("Change network state:" + currNetworkState);

            if (NetworkStateChangedEvent != null)
            {
                NetworkStateChangedEvent(newState);
            }
        }

        public void StartReceivePacket()
        {
             Thread t = new Thread(receivePacket);
             t.Start();
        }

        private void receivePacket()
        {
            for (;;)
            {
                Debug.Log("receive pakcet...");
                Packet p = transport.ReadPacket();
                if (p == null)
                {
                    Debug.LogWarning("receive packet errror...");
                    break;
                }
                Debug.Log("receive a new packet:" + p.ToString());
                ProcessPacket(p);

            }
        }

        public void On(string route, Action<Dictionary<string, object>> action)
        {
            this.eventMgr.AddOnEvent(route, action);
        }

        public void Request(string route, object msg, Action<Dictionary<string, object>> action)
        {
            this.eventMgr.AddCallback(reqId, action);
            Packet p = new Packet();
            p.packetType = PacketType.Req;
            p.id = reqId;
            p.topic = route;
            p.payload = protocol.Marshal(msg);
            this.transport.WritePacket(p);
            reqId++;
            if (reqId >= Int32.MaxValue) {
                reqId = 1;
            }
        }

        /**
         * 执行packet
         * @param p : packet instance
         */
        public void ProcessPacket(Packet p)
        {
            if (p.packetType == PacketType.Response)
            {
                eventMgr.InvokeCallback(p.id, p.payload);
            }
            else if (p.packetType == PacketType.Push)
            {
                eventMgr.InvokeOnEvent(p.topic, p.payload);
            }
            else
            {
                Debug.LogError("unknown packet type.=" + p.packetType);
            }
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}