using System;
using LiteNetLib.Utils;

//Some code parts taked from lidgren-network-gen3

namespace LiteNetLib
{
    public sealed class NatPunchModule
    {
        private readonly NetBase _netBase;
        private readonly NetSocket _socket;

        internal NatPunchModule(NetBase netBase, NetSocket socket)
        {
            _netBase = netBase;
            _socket = socket;
        }

        public void NatIntroduce(
            NetEndPoint hostInternal,
            NetEndPoint hostExternal,
            NetEndPoint clientInternal,
            NetEndPoint clientExternal,
            string additionalInfo)
        {
            NetPacket p = new NetPacket();
            NetDataWriter dw = new NetDataWriter();

            //First packet (server)
            dw.Put((byte)0);
            dw.Put(hostInternal);
            dw.Put(hostExternal);
            dw.Put(additionalInfo);

            p.Init(PacketProperty.NatIntroduction, dw);
            _socket.SendTo(p.RawData, clientExternal);

            //Second packet (client)
            dw.Reset();
            dw.Put((byte)1);
            dw.Put(clientInternal);
            dw.Put(clientExternal);
            dw.Put(additionalInfo);

            p.Init(PacketProperty.NatIntroduction, dw);
            _socket.SendTo(p.RawData, hostExternal);
        }

        public void SendNatIntroduceRequest(NetEndPoint masterServerEndPoint, string additionalInfo)
        {
            if (!_netBase.IsRunning)
                return;

            //prepare outgoing data
            NetDataWriter dw = new NetDataWriter();
            dw.Put(_netBase.LocalEndPoint);
            dw.Put(additionalInfo);

            //prepare packet
            NetPacket p = new NetPacket();
            p.Init(PacketProperty.NatIntroductionRequest, dw);
            _socket.SendTo(p.RawData, masterServerEndPoint);
        }

        internal void ProcessMessage(NetEndPoint senderEndPoint, PacketProperty property, byte[] data)
        {
            NetDataReader dr = new NetDataReader(data);
            NetDataWriter dw = new NetDataWriter();

            switch (property)
            {
                case PacketProperty.NatIntroductionRequest:
                    //TODO!
                    break;
                case PacketProperty.NatIntroduction:
                    //TODO!
                    break;
                case PacketProperty.NatPunchMessage:
                    byte fromHostByte = dr.GetByte();
                    if (fromHostByte == 0)
                    {
                        //it's from client
                        return;
                    }
                    string additionalInfo = dr.GetString(1000);

                    NetUtils.DebugWrite(ConsoleColor.Green, "NAT punch received from {0} we're client, so we've succeeded - additional info: {1}", senderEndPoint, additionalInfo);

                    //Release punch success to client; enabling him to Connect() to msg.Sender if token is ok
                    var netEvent = _netBase.CreateEvent(NetEventType.NatIntroductionSuccess);
                    netEvent.RemoteEndPoint = senderEndPoint;
                    _netBase.EnqueueEvent(netEvent);

                    //send a return punch just for good measure
                    dw.Put((byte)0);
                    dw.Put(additionalInfo);
                    NetPacket packet = new NetPacket();
                    packet.Init(PacketProperty.NatPunchMessage, dw);
                    _socket.SendTo(packet.RawData, senderEndPoint);
                    break;
            }
        }
    }
}
