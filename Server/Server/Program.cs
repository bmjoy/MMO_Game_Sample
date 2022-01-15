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
	// 현재 관리 중이 쓰레드 리스트

	// 1. Recv (N개)		=> 고객에게 주문을 받는 직원
	// 2. GameLogic (1개)	=> 요리를 하는 직원
	// 3. Send (1개)		=> 요리사에게 주문을 전달하는 직원
	// 4. DB (1개)			=> 결제를 하는 직원

	class Program
	{
		static Listener _listener = new Listener();
		static void GameLogicTask()
		{
			while (true)
			{
				GameLogic.Instance.Update();
				Thread.Sleep(0);
			}
		}

		static void DbTask()
		{
			while (true)
			{
				// DbTransaction.Flush를 실행하게 되면 
				// 내부에서 while문을 돌면서 일감이 없을 때까지 계속 처리를 하다가
				// 일감이 없을 때 DbTransaction.Flush() 처리가 끝나게 되고
				// Thread.Sleep(0)를 해줌으로써 제어권을 커널에 잠시 넘기는 방식을 사용해보자.
				// 이렇게 하면 Cpu가 너무 과하게 일을 하는 것을 우회할 수 있게 된다.
				DbTransaction.Instance.Flush();
				Thread.Sleep(0);
			}
		}

		static void NetworkTask()
		{
			while (true)
			{
				List<ClientSession> sessions = SessionManager.Instance.GetClientSessions();
				foreach (ClientSession session in sessions)
				{
					session.FlushSend();
				}
				Thread.Sleep(0);
			}
		}

		static void Main(string[] args)
		{
			ConfigManager.LoadConfig();
			DataManager.LoadData();

			// GameRoom 생성
			GameLogic.Instance.Push(() => { GameRoom room = GameLogic.Instance.Add(1); });

			// DNS (Domain Name System)
			string host = Dns.GetHostName();
			IPHostEntry ipHost = Dns.GetHostEntry(host);
			IPAddress ipAddr = ipHost.AddressList[1];
			IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

			_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
			Console.WriteLine("Listening...");

			// GameLogic을 처리하는 일꾼
			{
				// GameLogic을 처리하는 직원을 채용한 다음에 처리하는 방식으로
				// Thread에 이름을 할당해주면 디버깅을 할 때 조금 더 편리하다.
				Thread t = new Thread(DbTask);
				t.Name = "DB";
				t.Start();
			}

			// Network 일감을 처리하는 일꾼
			{
				Thread t = new Thread(NetworkTask);
				t.Name = "Network";
				t.Start();
			}

			// GameLogic을 처리하는 부분은 main Thread에서 담당시켜주자			
			Thread.CurrentThread.Name ="GameLogic";
			GameLogicTask();
		}
	}
}
