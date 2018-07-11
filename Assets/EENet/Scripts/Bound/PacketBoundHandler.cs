using System;
using System.IO;



namespace EENet
{
    class Stateobject
    {
        public const int BufferSize = 1024;
        internal byte[] buffer = new byte[BufferSize];
    }

    public class PacketBoundHandler : IBound
    {

        public const int MAX_PACKET_LENGTH_SIZE = 4;

        ITransport tranport;

        private Stateobject stateObject = new Stateobject();

        private IAsyncResult asyncSend;

        private IAsyncResult asyncReceive;

        private bool onSending = false;

        private bool onReceiving = false;

        private byte packetHead = 0;

        private byte[] headBuffer = new byte[MAX_PACKET_LENGTH_SIZE];

        private int headOffset = 0;


        private byte[] buffer;

        private int bufferOffset = 0;

        private System.Object _lock = new System.Object();

        private InboundState inboundState;

        private Action<byte[]>  rcvPacketCallback;


        public PacketBoundHandler(ITransport tranport, Action<byte[]> rcvCallback)
        {
            this.tranport = tranport;
            this.inboundState = InboundState.readHead;
            this.rcvPacketCallback = rcvCallback;
        }

        public void Decode()
        {
            this.asyncReceive = this.tranport.GetReader().BaseStream.BeginRead(this.stateObject.buffer, 0, this.stateObject.buffer.Length, new AsyncCallback(endReceive), stateObject);
            this.onReceiving = true;
        }

        private void endReceive(IAsyncResult asyncResult)
        {
            if (this.inboundState == InboundState.closed)
            {
                return;
            }
            Stateobject state = (Stateobject)asyncResult.AsyncState;
            try
            {
                int readLength = this.tranport.GetReader().BaseStream.EndRead(asyncResult);
                this.onReceiving = false;
                if (readLength > 0)
                {
                    this.processBytes(state.buffer, 0, readLength);
                    if (this.inboundState != InboundState.closed)
                    {
                        Decode();
                    }
                }
                else
                {
                    this.tranport.OnDisconnect();
                }

            }
            catch (Exception)
            {
                this.tranport.OnDisconnect();
            }
        }

        /**
         *process recevied bytes
         * @param data  received data
         * @param offset
         * @param limit
         */
        internal void processBytes(byte[] data, int offset, int limit)
        {
            if (this.inboundState == InboundState.readHead)
            {
                readHead(data, offset, limit);
            }
            else if (this.inboundState == InboundState.readLength)
            {
                readPacketLength(data, offset, limit);
            }
            else if (this.inboundState == InboundState.readBody)
            {
                readBody(data, offset, limit);
            }

        }

        private bool  readBody(byte[] data, int offset, int limit)
        {
            int length = limit - offset;
            if ( length >= (buffer.Length - bufferOffset) )
            {
                Array.Copy(data, offset, buffer, bufferOffset , buffer.Length - bufferOffset);
                offset += (buffer.Length - bufferOffset);
                // 处理message
                this.bufferOffset = 0;
                this.headOffset = 0;
                this.packetHead = 0;
                rcvPacketCallback.Invoke(buffer);
                
                this.inboundState = InboundState.readHead;
                if (offset <= limit) processBytes(data, offset, limit);
                return true;
            }
            else
            {
                Array.Copy(data, offset, buffer, bufferOffset, limit - offset);
                bufferOffset += (limit - offset);
                return false;
            }
        }

        /**
         *
         */
        private bool readHead(byte[] data, int offset, int limit)
        {
            int length = limit - offset;
            if (length > 0)
            {
                this.packetHead = data[0];
                this.inboundState = InboundState.readLength;
                offset++;

                if (limit > offset) processBytes(data, offset, limit);
                return true;
            }
            return false;
        }

        private bool readPacketLength(byte[] data, int offset, int limit)
        {
            if (limit <= offset) return false;
            for (; ; )
            {
                byte dataIndex = data[offset];
                offset++;
                headBuffer[headOffset] = dataIndex;
                headOffset++;
                if ((dataIndex & 128) != 0)
                {
                    if (headOffset >= MAX_PACKET_LENGTH_SIZE)
                    {
                        throw new PacketException("Invalid input stream");
                    }
                    if (limit <= offset) break;
                }
                else
                {
                     UInt32 rLength = 0;
                    int multiplier = 0;
                    int headIndex = 0;
                    while (headIndex <= headOffset)
                    {
                        byte val = headBuffer[headIndex];
                        rLength |= ((UInt32)(val & 127)) << multiplier;
                        headIndex ++;
                        if ((val & 128) == 0)
                        {
                            break;
                        }
                        multiplier += 7;
                        
                    }
                    buffer = new byte[rLength + 1 + headIndex];
                    buffer[0] = this.packetHead;
                    Array.Copy(headBuffer, 0, buffer, 1, headIndex);
                    bufferOffset = 1 + headIndex;
                    this.inboundState = InboundState.readBody;
                    if (offset <= limit) processBytes(data, offset, limit);
                    return true;
                }
            }
            return false;
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            this.inboundState = InboundState.closed;
        }

        public byte[] Encode(Packet p)
        {
            return p.ToBytes();
        }
    }
}