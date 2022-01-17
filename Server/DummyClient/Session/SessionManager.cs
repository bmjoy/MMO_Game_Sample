using System;
using System.Collections.Generic;

namespace DummyClient.Session
{
    // 여러 더미 클라이언트들이 각자 하나의 서버와 연결이 되어 있는 상태를 관리
    public class SessionManager
    {
        public static SessionManager Instance { get; } = new SessionManager();

        HashSet<ServerSession> _sessions = new HashSet<ServerSession>();
        object _lock = new Object();
        int _dummyId = 1;

        public ServerSession Generate()
        {
            lock (_lock)
            {
                ServerSession session = new ServerSession();
                session.DummyId = _dummyId;
                _dummyId++;

                _sessions.Add(session);
                Console.WriteLine($"Connected ({_sessions.Count}) Players");
                return session;
            }
        }

        public void Remove(ServerSession session)
        {
            lock (_lock)
            {
                _sessions.Remove(session);
                Console.WriteLine($"Connected ({_sessions.Count}) Players");
            }
        }
    }
}