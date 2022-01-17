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
        public const int VisionCells = 5;
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
            return GetZone(y, x);
        }

        public Zone GetZone(int indexY, int indexX)
        {
            if (indexX < 0 || indexX >= Zones.GetLength(1))
                return null;
            if (indexY < 0 || indexY >= Zones.GetLength(0))
                return null;
            
            return Zones[indexY, indexX];
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
            for (int i = 0; i < 500; i++)
            {
                Monster monster = ObjectManager.Instance.Add<Monster>();
                monster.Init(1);
                EnterGame(monster, randomPos: true);
            }
        }

        // 누군가가 주기적으로 호출해줘야 한다.
        public void Update()
        {
            Flush();
        }

        public void EnterGame(GameObject gameObject, bool randomPos)
        {
            if (gameObject == null)
                return;
            Random _rand = new Random();
            Vector2Int respawnPos;

            if (randomPos)
            {
                while (true)
                {
                    respawnPos.x = _rand.Next(Map.MinX, Map.MaxX + 1);
                    respawnPos.y = _rand.Next(Map.MinY, Map.MaxY + 1);
                    if (Map.Find(respawnPos) == null)
                    {
                        gameObject.CellPos = respawnPos;
                        break;
                    }
                }
            }

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

                    // 내 시야각에 있는 object 정보만 전송
                    player.Vision.Update();
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = gameObject as Monster;
                _monsters.Add(gameObject.Id, monster);
                monster.Room = this;
                GetZone(monster.CellPos).Monsters.Add(monster);
                Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));
                monster.Update();
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = gameObject as Projectile;
                _projectiles.Add(gameObject.Id, projectile);
                projectile.Room = this;
                GetZone(projectile.CellPos).Projectiles.Add(projectile);
                projectile.Update();
            }
            // 타인한테 정보 전송
            {
                S_Spawn spawnPacket = new S_Spawn();
                spawnPacket.Objects.Add(gameObject.Info);
                Broadcast(gameObject.CellPos, spawnPacket);
            }
        }

        public void LeaveGame(int objectId)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(objectId);
            Vector2Int cellPos;
            if (type == GameObjectType.Player)
            {
                Player player = null;
                if (_players.Remove(objectId, out player) == false)
                    return;
                cellPos = player.CellPos;

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
                cellPos = monster.CellPos;
                Map.ApplyLeave(monster);
                monster.Room = null;
            }
            else if (type == GameObjectType.Projectile)
            {
                Projectile projectile = null;
                if (_projectiles.Remove(objectId, out projectile) == false)
                    return;
                cellPos = projectile.CellPos;
                Map.ApplyLeave(projectile);
                projectile.Room = null;
            }
            else
            {
                return;
            }

            // 타인한테 정보 전송
            {
                S_Despawn despawnPacket = new S_Despawn();
                despawnPacket.ObjectIds.Add(objectId);
                Broadcast(cellPos, despawnPacket);
            }
        }
        
        // Todo
        Player FindPlayer(Func<GameObject, bool> condition)
        {
            foreach (Player player in _players.Values)
            {
                if (condition.Invoke(player))
                    return player;
            }

            return null;
        }

        // A*를 호출하기 때문에 살짝 부담스러운 함수
        public Player FindClosestPlayer(Vector2Int pos, int range)
        {
            List<Player> players = GetAdjacentPlayer(pos, range);
            
            // 가장 가까운 순서대로 정렬
            players.Sort((left, right) => 
            {
                int leftDist = (left.CellPos - pos).cellDistFromZero;
                int rightDist = (right.CellPos - pos).cellDistFromZero;
                return leftDist - rightDist;
            });

            foreach (Player player in players)
            {
                List<Vector2Int> path = Map.FindPath(pos, player.CellPos, checkObjects: true);

                // 갈 수 있는 길이 없다 || 너무 멀리 떨어져 있다.
                if (path.Count < 2 || path.Count > range)
                    continue;
                
                return player;
            }
            
            return null;
        }

        public void Broadcast(Vector2Int pos, IMessage packet)
        {
            List<Zone> zones = GetAdjacentZones(pos);
            
            foreach (Player p in zones.SelectMany(z => z.Players))
            {
                int dx = p.CellPos.x - pos.x;
                int dy = p.CellPos.y - pos.y;
                // 자신과 대상의 거리가 VisionCells보다 크면 => 범위에서 벗어나면 제외 
                if (Math.Abs(dx) > GameRoom.VisionCells)
                    continue;
                if (Math.Abs(dy) > GameRoom.VisionCells)
                    continue;
                
                p.Session.Send(packet);
            }
        }

        public List<Player> GetAdjacentPlayer(Vector2Int pos, int range)
        {
            List<Zone> zones = GetAdjacentZones(pos, range);
            return zones.SelectMany(z => z.Players).ToList();
        }

        // Broadcasting을 할 때 인접한 Zone에 있는지 체크
        // ㅁX X X ㅁ
        // ㅁX A X ㅁ
        // ㅁX X X ㅁ
        // ㅁㅁㅁㅁㅁ
        // A지점을 기준으로 좌측 상단 우측 하단에 있는 Zone을 긁어온 다음
        // 좌측 상단과 우측 하단의 대각선 내부에 있는 사각형 범위에 있는 모든 Zone을 긁어와준다.
        // 즉 시야 범위가 상하좌우에 있는 존을 벗어날 수도 있는 경우를 대비해서 만들어줌
        public List<Zone> GetAdjacentZones(Vector2Int cellPos, int range = GameRoom.VisionCells)
        {
            HashSet<Zone> zones = new HashSet<Zone>();
            int maxY = cellPos.y + range;
            int minY = cellPos.y - range;
            int maxX = cellPos.x + range;
            int minX = cellPos.x - range;

            // 좌측 상단
            Vector2Int leftTop = new Vector2Int(minX, maxY);
			int minIndexY = (Map.MaxY - leftTop.y) / ZoneCells;
            int minIndexX = (leftTop.x - Map.MinX) / ZoneCells; 

            // 우측 하단
            Vector2Int rightBot = new Vector2Int(maxX, minY);
			int maxIndexY = (Map.MaxY - rightBot.y) / ZoneCells;
            int maxIndexX = (rightBot.x - Map.MinX) / ZoneCells; 

            for (int x = minIndexX; x <= maxIndexX; x++)
            {
                for (int y = minIndexY; y <= maxIndexY; y++)
                {
                    Zone zone = GetZone(y, x);
                    if (zone == null)
                        continue;
                    zones.Add(zone);
                }
            }

            return zones.ToList();
        }
    }
}
