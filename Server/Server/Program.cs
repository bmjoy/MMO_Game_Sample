using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using ServerCore;
using Server.Game;
using Server.Data;
using Server.DB;
using System.Linq;

namespace Server
{
	class Program
	{
		static Listener _listener = new Listener();

		// 현재 돌고 있는 타이머들을 보관
		static List<System.Timers.Timer> _timers = new List<System.Timers.Timer>();
		
		static void TickRoom(GameRoom room, int tick = 100)
		{
			var timer = new System.Timers.Timer();
			timer.Interval = tick;
			// 특정 시간이 지났다면 어떤 이벤트를 실행할 것인지를 결정
			timer.Elapsed += ((s, e) => { room.Update(); });
			// 자동으로 리셋
			timer.AutoReset = true;

			// Enabled이 true가 되면 시작
			timer.Enabled = true;

			_timers.Add(timer);
		}

		static void Main(string[] args)
		{
			ConfigManager.LoadConfig();
			DataManager.LoadData();
			
			// GameRoom 생성
			GameRoom room = RoomManager.Instance.Add(1);
			TickRoom(room, 50);

			// DNS (Domain Name System)
			string host = Dns.GetHostName();
			IPHostEntry ipHost = Dns.GetHostEntry(host);
			IPAddress ipAddr = ipHost.AddressList[1];
			IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

			_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
			Console.WriteLine("Listening...");

			//FlushRoom();
			//JobTimer.Instance.Push(FlushRoom);

			// Todo
			while (true)
			{
				//JobTimer.Instance.Flush();
				DbTransaction.Instance.Flush();
			}
		}
	}
}
