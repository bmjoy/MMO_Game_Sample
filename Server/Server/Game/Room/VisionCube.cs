using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game
{
    public class VisionCube
    {
        public Player Owner { get; private set; }
        public HashSet<GameObject> PreviousObject { get; private set; } = new HashSet<GameObject>();

        public VisionCube(Player owner)
        {
            Owner = owner;
        }

        public HashSet<GameObject> GatherObject()
        {
            // 차후 Job 방식으로 처리해주기 위한 null 체크
            if (Owner == null || Owner.Room == null)
                return null;
            HashSet<GameObject> objects = new HashSet<GameObject>();

            // 내 주변에 있는 시야각을 가지고 온다.
            Vector2Int cellPos = Owner.CellPos;
            List<Zone> zones = Owner.Room.GetAdjacentZones(cellPos);
            
            // 인접한 zone에 있는 player들 중에서 자신의 시야각에 있는 player의 정보만 긁어온다.
            foreach (Zone zone in zones)
            {
                foreach (Player player in zone.Players)
                {
                    // 자신의 
                    int dx = player.CellPos.x - cellPos.x;
                    int dy = player.CellPos.y - cellPos.y;
                    // 자신과 대상의 거리가 VisionCells보다 크면 => 범위에서 벗어나면 제외 
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;

                    // 자신의 시야각에 있는 대상을 추가
                    objects.Add(player);
                }

                foreach (Monster monster in zone.Monsters)
                {
                    // 자신의 
                    int dx = monster.CellPos.x - cellPos.x;
                    int dy = monster.CellPos.y - cellPos.y;
                    // 자신과 대상의 거리가 VisionCells보다 크면 => 범위에서 벗어나면 제외
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;
                    
                    // 자신의 시야각에 있는 대상을 추가
                    objects.Add(monster);
                }

                foreach (Projectile projectile in zone.Projectiles)
                {
                    // 자신의 
                    int dx = projectile.CellPos.x - cellPos.x;
                    int dy = projectile.CellPos.y - cellPos.y;
                    // 자신과 대상의 거리가 VisionCells보다 크면 => 범위에서 벗어나면 제외 
                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;
                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;

                    // 자신의 시야각에 있는 대상을 추가
                    objects.Add(projectile);
                }
            }
            
            return objects;
        }

        // Before & After를 비교해서 
        public void Update()
        {
            if (Owner == null || Owner.Room == null)
                return;
            
            // Update가 되는 시점에 시야각에 있는 모든 object들을 긁어온다.
            HashSet<GameObject> currentObjects = GatherObject();
            
            // 기존엔 없었는데 새로 생긴 object가 있다면 Spawn

            // currentObjects - PreviousObject = 새로 추가된 object
            List<GameObject> added = currentObjects.Except(PreviousObject).ToList();
            if (added.Count > 0)
            {
                S_Spawn spawnPacket = new S_Spawn();
                foreach (GameObject gameObject in added)
                {
                    // MergeFrom을 하는 이유
                    // 참조 값(gameObject.Info)을 곧 바로 넣어주게 되면
                    // 해당 패킷이 곧바로 가는 것이 아니기 때문에 
                    // 참조 값으로 바로 보내주면 에러가 발생할 수 있게 된다.
                    ObjectInfo info = new ObjectInfo();
                    info.MergeFrom(gameObject.Info);
                    spawnPacket.Objects.Add(info);
                }
                Owner.Session.Send(spawnPacket);
            }

            // 기존엔 있었는데 사라진 object는 Despawn

            // PreviousObject - currentObjects = 사라진 object
            List<GameObject> removed = PreviousObject.Except(currentObjects).ToList();
            if (removed.Count > 0)
            {
                S_Despawn despawnPacket = new S_Despawn();
                foreach (GameObject gameObject in removed)
                {
                    despawnPacket.ObjectIds.Add(gameObject.Id);
                }
                Owner.Session.Send(despawnPacket);
            }

            PreviousObject = currentObjects;

            // 0.5뒤 다시 실행
            Owner.Room.PushAfter(500, Update);
        }
    }
}