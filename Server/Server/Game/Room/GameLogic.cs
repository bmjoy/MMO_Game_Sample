using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    // GameRoom의 Update를 처리하는 것이 아니라 GameLogic의 Update를 처리할 예정
    public class GameLogic : JobSerializer
    {
        public static GameLogic Instance { get; } = new GameLogic();
        Dictionary<int, GameRoom> _rooms = new Dictionary<int, GameRoom>();
        int _roomId = 1;

        // GameRoom을 돌면서 Update를 실행
        public void Update()
        {
            Flush();

            foreach (GameRoom room in _rooms.Values)
            {
                room.Update();
            }
        }

        public GameRoom Add(int mapId)
        {
            GameRoom gameRoom = new GameRoom();

            // gameRoom.Push<int>(gameRoom.Init, mapId); 을 줄여서 아래와 같이 구현
            gameRoom.Push(gameRoom.Init, mapId, 10);

            // atomic하게 lock을 잡고 작업이 진행되기 때문에
            // roomId가 중복해서 증가되는 일은 없을 것이다.
            gameRoom.RoomId = _roomId;
            _rooms.Add(_roomId, gameRoom);
            _roomId++;
            return gameRoom;
        }

        public bool Remove(int roomId)
        {
            return _rooms.Remove(roomId);
        }

        public GameRoom Find(int roomId)
        {
            GameRoom room = null;
            if (_rooms.TryGetValue(roomId, out room))
                return room;

            return null;
        }
    }
}
