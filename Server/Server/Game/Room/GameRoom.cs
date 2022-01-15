using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game
{
    public partial class GameRoom : JobSerializer
    {
        public int RoomId { get; set; }

        // 경우에 따라서 Dic로 관리 => int, Player => playerID에 해당하는 정보를 맵핑
        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
        Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();

        public Zone[,] Zones { get; private set; }
        // 하나의 Zone이 몇칸인지 구분
        public int ZoneCells { get; private set; }
        public Map Map { get; private set; } = new Map();

        // ㅁㅁㅁ
        // ㅁㅁㅁ
        // ㅁㅁㅁ
        public Zone GetZone(Vector2Int cellPos)
        {
            // Cell을 계산을 할 때 주의
            // 열의 증가 => X가 오름차순으로 증가하는 식
            // 행의 증가 => Y가 내림차순으로 감소하는 식

            // ZoneIndex로 넘어가기 위해서는 ZoneCell 단위로 나눠줘야 한다.
            // (A) / B => A: 전체 맵 크기에서 x, y좌표 / 현재 Zone의 x축 Cell 크기
            int x = (cellPos.x - Map.MinX) / ZoneCells; 
			int y = (Map.MaxY - cellPos.y) / ZoneCells;

            if (x < 0 || x >= Zones.GetLength(1))
                return null;
            if (y < 0 || y >= Zones.GetLength(0))
                return null;
            
            return Zones[y, x];
        }
        public void Init(int mapId, int zoneCells)
        {
            Map.LoadMap(mapId);
            // Zone
            ZoneCells = zoneCells; // 10
            // 전체 맵 크기 1 ~ 10칸  = 1개 존
            // 전체 맵 크기 11 ~ 20칸 = 2개 존
            // 전체 맵 크기 21 ~ 30칸 = 3개 존
            int countY = (Map.SizeY + zoneCells -1) / zoneCells;
            int countX = (Map.SizeX + zoneCells -1) / zoneCells;
            Zones = new Zone[countY, countX];
            for (int y = 0; y < countY; y++)
            {
                for (int x = 0; x < countX; x++)
                {
                    Zones[y, x] = new Zone(y, x);
                }
            }

            // TEMP
            Monster monster = ObjectManager.Instance.Add<Monster>();
            monster.Init(1);
            monster.CellPos = new Vector2Int(5, 5);
            EnterGame(monster);
        }

        // 누군가가 주기적으로 호출해줘야 한다.
        public void Update()
        {
            Flush();
        }

        public void EnterGame(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

            if (type == GameObjectType.Player)
            {
                Player player = gameObject as Player;
                _players.Add(gameObject.Id, player);
                player.Room = this;

                // DB에서 가지고 온 아이템 정보를 통해서 player의 Stat 정보를 초기화
                // 방에 입장했을 때 한번 정도 초기화를 해준다고 생각
                player.RefreshAdditionalStat();

                // 처음 접속하자마자 데미지가 안들어오는 버그 수정
                Map.ApplyMove(player, new Vector2Int(player.CellPos.x, player.CellPos.y));
                GetZone(player.CellPos).Players.Add(player);

                // 본인한테 정보 전송
                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = player.Info;
                    player.Session.Send(enterPacket);

                    // 해당 방에 접속한 플레이어의 정보 전송
                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (Player p in _players.Values)
                    {
                        if (player != p)
                            spawnPacket.Objects.Add(p.Info);
                    }

                    // 해당 방에 있는 몬스터의 정보도 보내줘야 한다.
                    // 방금 막 방에 접속한 플레이어는 몬스터 정보를 볼 수 없는 문제가 생김
                    foreach (Monster m in _monsters.Values)
                        spawnPacket.Objects.Add(m.Info);

                    foreach (Projectile p in _projectiles.Values)
                        spawnPacket.Objects.Add(p.Info);

                    player.Session.Send(spawnPacket);
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = gameObject as Monster;
                _monsters.Add(gameObject.Id, monster);
                monster.Room = this;

                Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));
                monster.Update();
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = gameObject as Projectile;
                _projectiles.Add(gameObject.Id, projectile);
                projectile.Room = this;

                projectile.Update();
            }
                
            // 타인한테 정보 전송
            {
                S_Spawn spawnPacket = new S_Spawn();
                spawnPacket.Objects.Add(gameObject.Info);
                foreach (Player p in _players.Values)
                {
                    // 자신을 제외한 모두에게 발송
                    if (p.Id != gameObject.Id)
                        p.Session.Send(spawnPacket);
                }
            }
        }

        public void LeaveGame(int objectId)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

            if (type == GameObjectType.Player)
            {
                Player player = null;
                if (_players.Remove(objectId, out player) == false)
                    return;

                GetZone(player.CellPos).Players.Remove(player);
                player.OnLeaveGame();
                Map.ApplyLeave(player);
                player.Room = null;

                // 본인한테 정보 전송
                {
                    // S_LeaveGame 패킷을 받으면 자신이 나간 것이기 때문에
                    // 별도의 추가 작업이 필요 없다.
                    S_LeaveGame leavePacket = new S_LeaveGame();
                    player.Session.Send(leavePacket);
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = null;
                if (_monsters.Remove(objectId, out monster) == false)
                    return;

                Map.ApplyLeave(monster);
                monster.Room = null;
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = null;
                if (_projectiles.Remove(objectId, out projectile) == false)
                    return;

                projectile.Room = null;
            }

            // 타인한테 정보 전송
            {
                // 다른 플레이어가 나갔다면 DESPWAN을 해줘야 한다.
                S_Despawn despawnPacket = new S_Despawn();
                despawnPacket.ObjectIds.Add(objectId);
                foreach (Player p in _players.Values)
                {
                    if (p.Id != objectId)
                        p.Session.Send(despawnPacket);
                }
            }
        }
        
        // Todo
        public Player FindPlayer(Func<GameObject, bool> condition)
        {
            foreach (Player player in _players.Values)
            {
                if (condition.Invoke(player))
                    return player;
            }

            return null;
        }

        public void Broadcast(Vector2Int pos, IMessage packet)
        {
            List<Zone> zones = GetAdjacentZones(pos);
            
            foreach (Player p in zones.SelectMany(z => z.Players))
            {
                p.Session.Send(packet);
            }
        }

        // Broadcasting을 할 때 인접한 Zone에 있는지 체크
        // 현재 위치를 기준으로 1, 2, 3, 4 분면에 있는 Zone의 위치를 전달
        // 보통은 1개의 Zone을 전달하겠지만 경계선에 있다면 2개 이상의 Zone을 전달
        public List<Zone> GetAdjacentZones(Vector2Int cellPos, int cells = 5)
        {
            HashSet<Zone> zones = new HashSet<Zone>();

            // 1, 2, 3, 4 분면에 있는 값
            int[] delta = new int[2] { -cells, +cells};
            foreach (int dy in delta)
            {
                foreach (int dx in delta)
                {
                    int y = cellPos.y + dy;
                    int x = cellPos.x + dx;
                    Zone zone = GetZone(new Vector2Int(x, y));
                    if (zone == null)
                        continue;
                    
                    zones.Add(zone);
                }
            }

            return zones.ToList();
        }
    }
}
