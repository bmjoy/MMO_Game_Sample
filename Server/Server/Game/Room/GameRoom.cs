using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class GameRoom
    {
        object _lock = new object();
        public int RoomId { get; set; }

        // 경우에 따라서 Dic로 관리 => int, Player => playerID에 해당하는 정보를 맵핑
        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
        Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();

        public Map Map { get; private set; } = new Map();

        public void Init(int mapId)
        {
            Map.LoadMap(mapId);

            // Temp
            Monster moster = ObjectManager.Instance.Add<Monster>();
            moster.CellPos = new Vector2Int(5, 5);
            EnterGame(moster);
        }

        public void Update()
        {
            lock (_lock)
            {
                foreach (Monster monster in _monsters.Values)
                {
                    monster.Update();
                }
                foreach (Projectile projectile in _projectiles.Values)
                {
                    projectile.Update();
                }
            }
        }
        
        public void EnterGame(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

            lock (_lock)
            {
                if (type == GameObjectType.Player)
                {
                    Player player = gameObject as Player;
                    _players.Add(gameObject.Id, player);
                    player.Room = this;

                    // 처음 접속하자마자 데미지가 안들어오는 버그 수정
                    Map.ApplyMove(player, new Vector2Int(player.CellPos.x, player.CellPos.y));

                    // 본인한테 정보 전송
                    {
                        S_EnterGame enterPacket = new S_EnterGame();
                        enterPacket.Player = gameObject.Info;
                        player.Session.Send(enterPacket);

                        // 해당 방에 접속한 플레이어의 정보 전송
                        S_Spawn spwanPacket = new S_Spawn();
                        foreach (Player p in _players.Values)
                        {
                            if (gameObject != p)
                                spwanPacket.Objects.Add(p.Info);
                        }

                        // 해당 방에 있는 몬스터의 정보도 보내줘야 한다.
                        // 방금 막 방에 접속한 플레이어는 몬스터 정보를 볼 수 없는 문제가 생김
                        foreach (Monster m in _monsters.Values)
                            spwanPacket.Objects.Add(m.Info);

                        foreach (Projectile p in _projectiles.Values)
                            spwanPacket.Objects.Add(p.Info);

                        player.Session.Send(spwanPacket);
                    }
                }
                else if (type == GameObjectType.Monster)
                {
                    Monster monster = gameObject as Monster;
                    _monsters.Add(gameObject.Id, monster);
                    monster.Room = this;

                    Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y));
                }
                else if (type == GameObjectType.Projectile)
                {
                    Projectile projectile = gameObject as Projectile;
                    _projectiles.Add(gameObject.Id, projectile);
                    projectile.Room = this;
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
        }

        public void LeaveGame(int objectId)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(objectId);
            lock (_lock)
            {
                if (type == GameObjectType.Player)
                {
                    Player player = null;
                    if (_players.Remove(objectId, out player) == false)
                        return;

                    player.Room = null;
                    Map.ApplyLeave(player);

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
                    monster.Room = null;
                    Map.ApplyLeave(monster);
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
        }
        
        public void HandleMove(Player player, C_Move movePacket)
        {
            if (player == null)
                return;

            lock (_lock)
            {
                // ToDO : 검증
                // 클라에서 거짓된 정보를 보냈을 수도 있다고 가정하고 한번 검증을 해줘야 한다.

                // 이동하길 원하는 위치
                PositionInfo movePosInfo = movePacket.PosInfo;
                // 현재 플레이어의 위치
                ObjectInfo info = player.Info;

                // 이동 검증
                // 이동하길 원하는 위치 != 현재 플레이어의 위치 => 이동을 했다는 의미
                if (movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
                {
                    if (Map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
                        return;
                }

                info.PosInfo.State = movePosInfo.State;
                info.PosInfo.MoveDir = movePosInfo.MoveDir;
                Map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));

                // Broadcast
                S_Move resMovePacket = new S_Move();
                resMovePacket.ObjectId = player.Info.ObjectId;
                resMovePacket.PosInfo = movePacket.PosInfo;

                Broadcast(resMovePacket);
            }
        }

        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            // 나중에는 player가 해당 Room에 실제로 존재하는지 더블 체크 하는 것도 좋다
            if (player == null)
                return;
            lock (_lock)
            {
                ObjectInfo info = player.Info;
                if (info.PosInfo.State != CreatureState.Idle)
                    return;
                // 통과
                info.PosInfo.State = CreatureState.Skill;

                S_Skill sKill = new S_Skill() { Info = new SkillInfo() };
                sKill.ObjectId = info.ObjectId;
                sKill.Info.SkillId = skillPacket.Info.SkillId; // 나중에 스킬과 관련된 부분은 데이터 시트(Json, XML로 따로 관리)로 관리 해서 관리
                Broadcast(sKill); // 스킬을 사용한다는 애니메이션을 맞추기 위한 Broadcast

                Data.Skill skillData = null;
                if (DataManager.SkillDict.TryGetValue(skillPacket.Info.SkillId, out skillData) == false)
                    return;

                switch (skillData.skllType)
                {
                    case SkillType.SkillAuto:
                        {
                            // Todo : 데미지 판정
                            Vector2Int skillPos = player.GetFrontCellPos(info.PosInfo.MoveDir);
                            GameObject target = Map.Find(skillPos);
                            if (target != null)
                            {
                                Console.WriteLine("Hit GameObject");
                            }
                        }
                        break;

                        // 투사체 스킬의 종류에 따라서 분기 처리를 해주는 작업을 해줘야 한다.
                    case SkillType.SkillProjectile:
                        {
                            // Todo : Arrow
                            Arrow arrow = ObjectManager.Instance.Add<Arrow>();
                            if (arrow == null)
                                return;

                            arrow.Owner = player;
                            arrow.Data = skillData;
                            arrow.PosInfo.State = CreatureState.Moving;
                            arrow.PosInfo.MoveDir = player.PosInfo.MoveDir;
                            arrow.PosInfo.PosX = player.PosInfo.PosX;
                            arrow.PosInfo.PosY = player.PosInfo.PosY;
                            arrow.Speed = skillData.projectile.speed;

                            EnterGame(arrow);
                        }
                        break;

                }
            }
        }

        public Player FindPlayer(Func<GameObject, bool> condition)
        {
            foreach (Player player in _players.Values)
            {
                if (condition.Invoke(player))
                    return player;
            }
            return null;
        }

        public void Broadcast(IMessage packet)
        {
            // C_MoveHandler를 여러 쓰레드에서 처리를 할 것이기 때문에 lock을 잡아줘야 한다.
            lock (_lock)
            {
                foreach (Player p in _players.Values)
                {
                    p.Session.Send(packet);
                }
            }
        }
    }
}
