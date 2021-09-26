using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class ObjectManager
    {
        public static ObjectManager Instance { get; } = new ObjectManager();
        object _lock = new object();

        // 플레이어를 찾기 위함
        Dictionary<int, Player> _players = new Dictionary<int, Player>();

        // [UnUsed(1)][Type(7)][ID(24)]
        int _counter = 0; // ToDo

        // GameObject를 상속받은 다양한 객체들을 생성해주는 역할
        public T Add<T>() where T : GameObject, new()
        {
            T gameObject = new T();
            lock (_lock)
            {
                // ID 발급
                gameObject.Id = GenerateId(gameObject.ObjectType);

                if (gameObject.ObjectType == GameObjectType.Player)
                {
                    _players.Add(gameObject.Id, gameObject as Player);
                }
            }
            return gameObject;
        }

        int GenerateId(GameObjectType type)
        {
            lock (_lock)
            {
                // << 24 의미
                // type은 24번째 이후에 기입하기로 설계를 해놨기 때문에 
                // 왼쪽으로 24칸 이동 시켜준다
                // | (_counter++) 의미
                // | 연산자는 값을 덮어 씌우기 때문에 ID를 비트 단위로 덮어준다.
                return ((int)type << 24) | (_counter++);
            }
        }

        public static GameObjectType GetObjectTypeById(int id)
        {
            // 0x7F => 7비트 꽉
            // id에서 타입 데이터만 뽑아오기 위해서
            int type = (id >> 24) & 0x7F;
            return (GameObjectType)type;
        }

        public bool Remove(int objectId)
        {
            GameObjectType objectType = GetObjectTypeById(objectId);
            lock (_lock)
            {
                if (objectType == GameObjectType.Player)
                    return _players.Remove(objectId);
            }
            return false;
        }

        public Player Find(int objectId)
        {
            GameObjectType objectType = GetObjectTypeById(objectId);

            lock (_lock)
            {
                if (objectType == GameObjectType.Player)
                {
                    Player player = null;
                    if (_players.TryGetValue(objectId, out player))
                        return player;
                }
            }
            return null;
        }
    }
}
