using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server
{
	class SessionManager
	{
		static SessionManager _session = new SessionManager();
		public static SessionManager Instance { get { return _session; } }

		int _sessionId = 0;
		Dictionary<int, ClientSession> _sessions = new Dictionary<int, ClientSession>();
		object _lock = new object();

		public List<ClientSession> GetClientSessions()
		{
			// Dic 원본을 보낼 수 없는 이유는 
			// 원본을 보내는 순간에 수정이 일어날 수 있기 때문
			List<ClientSession> sessions = new List<ClientSession>();
			lock (_lock)
			{
				sessions = _sessions.Values.ToList();
			}
			return sessions;
		}
		public ClientSession Generate()
		{
			lock (_lock)
			{
				int sessionId = ++_sessionId;

				ClientSession session = new ClientSession();
				session.SessionId = sessionId;
				_sessions.Add(sessionId, session);

				System.Console.WriteLine($"Connected ({_sessions.Count}) Players");

				return session;
			}
		}

		public ClientSession Find(int id)
		{
			lock (_lock)
			{
				ClientSession session = null;
				_sessions.TryGetValue(id, out session);
				return session;
			}
		}

		public void Remove(ClientSession session)
		{
			lock (_lock)
			{
				_sessions.Remove(session.SessionId);
				System.Console.WriteLine($"Connected ({_sessions.Count}) Players");
			}
		}
	}
}
