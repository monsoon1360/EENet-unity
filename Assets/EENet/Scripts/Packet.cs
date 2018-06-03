using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Text;
using System.Net;

/***********************************************************************************************************************
 * packet definition
 * 1.  控制报文由以下四个部分组成
 * +---------------------------+-----------------------------------+
 * |    Fixed    head          |    固定包头，所有报文均报包含    |
 * |    Id   head              |    消息id,部分报文包含           |
 * |    Topic head            |    路由报头，部分报文包含         |
 * |     Payload               |    包体，部分报文包含            |
 * +---------------------------+----------------------------------+
 *
 * 2. Fixed head
 *    2.1.   fixed head 格式
 *           +----------------+-------------------+
 *           |   byte1        |    报文类型        |
 *           |   byte2..      |    剩余长度        |
 *           +----------------+-------------------+
 *    2.2.   报文类型数据格式
 *           +-----+-----+-----+-----+-----+-----+-----+-----+
 *           |  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
 *           +-----+-----+-----+-----+-----+-----+-----+-----+
 *           |            报文类型   |  r  |iflag| tflag| pflag|
 *           +-----+-----+-----+-----+-----+-----+-----+-----+
 *          注： 3保留
 *                iflag:  标示是否有id head报文  为1表示有
 *				  tflag:  标示是否有topic head报文  为1表示有
 *                pflag:  标示是否有payload报文   为1表示有
 *    2.2.  报文类型定义表格
 *           +-------------+------------+---------------+
 *           |  名称        |    值      |      描述     |
 *           +-------------+------------+---------------+
 *           |  REQ        |   0x01     |   请求消息     |
 *           |  RESPONSE   |   0x02     |   响应消息     |
 *           |  PUBLISH    |   0x03     |   通知消息     |
 *           |  PUSH       |   0x04     |   推送消息     |
 *           | PINGREQ     |   0x05     |   ping请求     |
 *           | PINGRES     |   0x06     |   ping响应     |
 *           | DISCONNECT  |   0x07     |   主动断开     |
 *           | CMD         |   0x08     |   系统内部命令 |
 *           +-------------+------------+---------------+
 *    2.3.  剩余长度
 *         剩余长度为数据即为Variable head和payload中的数据长度总和, 对于小于128的值它使用单字节编码，更大的值按下面的方式处理：
 *                   低7位有效位用于编码数据，最高有效位用于指示是否有更多的字节。 最大为4个字节
 * 3. Id head
 *         标识消息id,部分报文包含, 固定4个字节(Uint32)
 *           +----------------------+--------------------+
 *           |   报文类型            |   是否含有Id head  |
 *           +----------------------+--------------------+
 *           |   REQ                |        是          |
 *           |   RESPONSE           |        是          |
 *           |   PUBLISH            |        否          |
 *           |   PUSH               |        否          |
 *           |  PINGREQ             |        否          |
 *           |  PINGRES             |        否          |
 *           |  DISCONNECT          |        否          |
 *           +----------------------+--------------------+
 * 4. Topic Head
 *    4.1.  Topic head 格式
 *           +--------------------------------------------+
 *           |   byte1,2      |    router 长度字节数       |
 *           |   byte3..n     |    router名称             |
 *           +--------------------------------------------+
 *    4.2.  包含router head的报文表格
 *           +----------------------+--------------------+
 *           |   报文类型            |   是否含有router   |
 *           +----------------------+--------------------+
 *           |   REQ                |        是          |
 *           |   RESPONSE           |        否          |
 *           |   PUBLISH            |        是          |
 *           |   PUSH               |        是          |
 *           |  PINGREQ             |        否          |
 *           |  PINGRES             |        否          |
 *           |  DISCONNECT          |        否          |
 *           +----------------------+--------------------+
 * 5. Pay load
 *          Pay load为报文的实际包体,部分报文包含该内容
 *   5.1.   包含pad load的报文表格
 *           +----------------------+--------------------+
 *           |   报文类型           |   是否含有payload    |
 *           +----------------------+--------------------+
 *           |   REQ                |        是          |
 *           |   RESPONSE           |        是          |
 *           |   PUBLISH            |        可选        |
 *           |   PUSH               |        可选        |
 *           |  PINGREQ             |        否          |
 *           |  PINGRES             |        否          |
 *           |  DISCONNECT          |        否          |
 *           +----------------------+--------------------+

 ***********************************************************************************************************************/
namespace EENet
{

    public class Packet
    {
        // id head标识位
        const byte  HEAD_MASK_ID = 0x01 << 2;

        // topic标识位
        const byte  HEAD_MASK_TOPIC = 0x01 << 1;

        // payload标识位
        const byte  HEAD_MASK_PAYLOAD = 0x01 << 0;

        

        public byte packetType;

        public string topic;

        public byte[] payload;

        public UInt32 id;

        public Packet()
        {

        }

        public Packet(byte type)
        {
            this.packetType = type;
        }

        public static Packet ReadFromBinaryReader(BinaryReader br)
        {
            var fixHead = br.ReadByte();
            byte flag = (byte)(fixHead >> 4);

            if ( ! isPacketTypeValid(flag) ) {
                throw new PacketException("Invalid Packet Type");
            }
            UInt32 remainLength = DecodeLength(br);
            Packet p = new Packet();
            p.packetType = flag;
            // check id head exist or not
            if ( (fixHead & HEAD_MASK_ID) != 0)
            {
                UInt32 id = (UInt32) readInt32(br);
                remainLength -= 4;
                p.id = id;
            }
            // check topic head exist or not
            if ((fixHead & HEAD_MASK_TOPIC) != 0)
            {
                short topicLength = readInt16(br);
                remainLength -= 2;
                byte[] topicBytes = br.ReadBytes(topicLength);
                p.topic = Encoding.Default.GetString(topicBytes);
                remainLength -= (UInt32)topicLength;
            }
            // check payload
            if ( (fixHead & HEAD_MASK_PAYLOAD) != 0)
            {
                if (remainLength > 0)
                {
                   byte[] payloadBytes = br.ReadBytes((int)remainLength);
                   p.payload = payloadBytes;
                }
            }
            return p;
        }

        private static short readInt16(BinaryReader br)
        {
            return IPAddress.HostToNetworkOrder(br.ReadInt16());
        }

        private static Int32 readInt32(BinaryReader br)
        {
            return IPAddress.HostToNetworkOrder(br.ReadInt32());
        }

        private static Int64 readInt64(BinaryReader br)
        {
            return IPAddress.HostToNetworkOrder(br.ReadInt64());
        }


        public byte[] ToBytes()
        {
            // append fix head
            ByteBuffer bb = ByteBuffer.Create();
            byte flag = (byte) (this.packetType << 4);
            if (this.id > 0)
            {
                flag |= HEAD_MASK_ID;
            }
            if (! string.IsNullOrEmpty(this.topic))
            {
                flag |= HEAD_MASK_TOPIC;
            }
            if (payload != null && payload.Length > 0) {
                flag |= HEAD_MASK_PAYLOAD;
            }

            bb.WriteByte(flag);

            // write length
            int remainLength = 0;
            if (this.id > 0) {
                remainLength += 4;
            }
            if (! string.IsNullOrEmpty(this.topic)) {
                remainLength += 2 + this.topic.Length;
            }
            remainLength += this.payload.Length;
            bb.WriteBytes(EncodeLength(remainLength));

            // write id head
            if (this.id > 0)
            {
                bb.WriteUint(this.id);
            }

            // write topic head
            if (! string.IsNullOrEmpty(this.topic))
            {
                bb.WriteUshort((ushort) (this.topic.Length));
                bb.WriteString(this.topic);
            }

            // write payload
            if (this.payload.Length > 0)
            {
                bb.WriteBytes(this.payload);
            }

            return bb.ToArray();
        }

        public static UInt32 DecodeLength(BinaryReader br)
        {
            UInt32 rLength = 0;
            int multiplier = 0;
            while (multiplier < 27)
            {
                byte val = br.ReadByte();
                rLength |= ((UInt32)(val & 127)) << multiplier;
                if ((val & 128) == 0)
                {
                    break;
                }
                multiplier += 7;
            }
            return rLength;
        }

        public byte[] EncodeLength(int length)
        {
            byte[] encLength = new byte[4];
            byte digit;
            int byteIndex = 0;
            for (;;)
            {
                digit = (byte)(length % 128);
                length  /= 128;
                if (length > 0)
                {
                    digit |= 0x80;
                }
                encLength[byteIndex] = digit;
                byteIndex ++;
                if (length == 0)
                {
                    break;
                }
            }
            byte[]  resultByte = new byte[byteIndex];
            Array.Copy(encLength, resultByte, byteIndex);
            return resultByte;
        }

        static bool isPacketTypeValid(byte pt)
        {
            switch(pt)
            {
                case PacketType.Req:
                case PacketType.Response:
                case PacketType.Push:
                case PacketType.Publish:
                case PacketType.PingRes:
                case PacketType.PingReq:
                case PacketType.Disconnect:
                case PacketType.Cmd:
                    return true;
                default:
                    return false;
            }
        }


        public override string ToString() {
            return "packet=> [type=" + this.packetType + ", id=" + this.id + ", payload length=" + (this.payload == null ? 0 : this.payload.Length) + "]";
        }

    }

    
}

