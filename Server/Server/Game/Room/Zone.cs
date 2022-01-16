using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;

namespace Server.Game
{
    // Zone은 GameRoom을 쪼개서 관리하는 개념
    // 그렇다면 Zone을 누가 관리를 하면 좋을까?
    // => 맵에서 들고 있어도 괜찮고 GameRoom에서 들고 있어도 괜찮지만
    // => 가상의 개념이고 해당 Zone에 있는 Player 및 몬스터들을 찾기 위한 개념이기 때문에
    // => GameRoom에서 관리를 해보자.
    public class Zone
    {
        // 자기가 몇 번째 존인지 기입
        // 아래와 같이 Zone을 나눴을 때 몇 번째 Zone인지 저장
        // [1,1]  [1,2]  [1,3]  [1,4] 
        // [2,1]  [2,2]  [2,3]  [2,4]
        // [3,1]  [3,2]  [3,3]  [3,4]
        public int IndexY { get; private set; }
        public int IndexX { get; private set; }

        public HashSet<Player> Players { get; set; } = new HashSet<Player>();
        public HashSet<Monster> Monsters { get; set; } = new HashSet<Monster>();
        public HashSet<Projectile> Projectiles { get; set; } = new HashSet<Projectile>();

        public Zone(int y, int x)
        {
            IndexY = y;
            IndexX = x;
        }

        public void Remove(GameObject gameObject)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

            switch (type)
            {
                case GameObjectType.Player:
                    Players.Remove((Player)gameObject);
                    break;
                case GameObjectType.Monster:
                    Monsters.Remove((Monster)gameObject);
                    break;        
                case GameObjectType.Projectile:
                    Projectiles.Remove((Projectile)gameObject);
                    break;
            }
        }

        public Player FindOnePlayer(Func<Player, bool> condition)
        {
            foreach (Player player in Players)
            {
                if (condition.Invoke(player))
                {
                    return player;
                }
            }
            return null;
        }
        
        public List<Player> FindAllPlayer(Func<Player, bool> condition)
        {
            List<Player> findList = new List<Player>();
            foreach (Player player in Players)
            {
                if (condition.Invoke(player))
                {
                    findList.Add(player);
                }
            }
            return findList;
        }
    }
}