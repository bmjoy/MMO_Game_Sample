using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using System.Net;
using Google.Protobuf.Protocol;
using Google.Protobuf;
using Server.Game;
using Server.Data;

namespace Server
{
	public partial class ClientSession : PacketSession
	{
		public PlayerServerState ServerState { get; private set; } = PlayerServerState.ServerStateLogin;
		// 세션에서도 현재 Session이 관리하고 있는 Player 정보와 GameRoom 정보를
		// 들고 있다면 좀더 유용할 것
		public Player MyPlayer { get; set; }
		public int SessionId { get; set; }

		object _lock = new object();
		List<ArraySegment<byte>> _reserveQueue = new List<ArraySegment<byte>>();

		long _pingpongTick = 0;
		public void Ping()
		{
			if (_pingpongTick > 0)
			{
				long delta = (System.Environment.TickCount64 - _pingpongTick);
				// Ping을 보내고 Pong이 안온지 30초가 지났을 때
				if (delta > 30 * 1000)
				{
					System.Console.WriteLine("Disconnected by PingCheck");
					Disconnect();
					return;
				}
			}
			
			S_Ping pingPacket = new S_Ping();
			Send(pingPacket);

			GameLogic.Instance.PushAfter(5000, Ping);
		}
		
		public void HandPong()
		{
			_pingpongTick = System.Environment.TickCount64;
		}

		#region Network
		public void Send(IMessage packet)
		{
			string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
			MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);
			ushort size = (ushort)packet.CalculateSize();
			byte[] sendBuffer = new byte[size + 4];
			Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
			Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
			Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);

			lock (_lock)
			{
				_reserveQueue.Add(sendBuffer);
			}
			//Send(new ArraySegment<byte>(sendBuffer));
		}

		// 실제 네트워크 IO를 보내는 부분
		public void FlushSend()
		{
			List<ArraySegment<byte>> sendList = null;

			lock (_lock)
			{
				if (_reserveQueue.Count == 0)
					return;
				sendList = _reserveQueue;
				_reserveQueue = new List<ArraySegment<byte>>();
			}

			Send(sendList);
		}

		public override void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnConnected : {endPoint}");

			{
				S_Connected connectedPacket = new S_Connected();
				Send(connectedPacket);
			}

			GameLogic.Instance.PushAfter(5000, Ping);
		}

		public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
			GameLogic.Instance.Push(() => 
			{
				if (MyPlayer == null)
					return;
				GameRoom room = GameLogic.Instance.Find(1);
				room.Push(room.LeaveGame, MyPlayer.Info.ObjectId);
			});
			SessionManager.Instance.Remove(this);

			Console.WriteLine($"OnDisconnected : {endPoint}");
		}

		public override void OnSend(int numOfBytes)
		{
			//Console.WriteLine($"Transferred bytes: {numOfBytes}");
		}
		#endregion
	}
}
