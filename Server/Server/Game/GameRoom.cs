using Google.Protobuf;
using Google.Protobuf.Protocol;
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
        List<Player> _players = new List<Player>();
        
        public void EnterGame(Player newPlayer)
        {
            if (newPlayer == null)
                return;

            lock (_lock)
            {
                _players.Add(newPlayer);
                newPlayer.Room = this;

                // 본인한테 정보 전송
                {
                    S_EnterGame enterPacket = new S_EnterGame();
                    enterPacket.Player = newPlayer.Info;
                    newPlayer.Session.Send(enterPacket);

                    // 해당 방에 접속한 플레이어의 정보 전송
                    S_Spawn spwanPacket = new S_Spawn();
                    foreach (Player p in _players)
                    {
                        if (newPlayer != p)
                            spwanPacket.Players.Add(p.Info);
                    }
                    newPlayer.Session.Send(spwanPacket);
                }

                // 타인한테 정보 전송
                {
                    S_Spawn spawnPacket = new S_Spawn();
                    spawnPacket.Players.Add(newPlayer.Info);
                    foreach (Player p in _players)
                    {
                        if (newPlayer != p)
                            p.Session.Send(spawnPacket);
                    }
                }
            }
        }

        public void LeaveGame(int playerId)
        {
            lock (_lock)
            {
                Player player = _players.Find(p => p.Info.PlayerId == playerId);
                if (player == null)
                    return;

                _players.Remove(player);
                player.Room = null;

                // 본인한테 정보 전송
                {
                    // S_LeaveGame 패킷을 받으면 자신이 나간 것이기 때문에
                    // 별도의 추가 작업이 필요 없다.
                    S_LeaveGame leavePacket = new S_LeaveGame();
                    player.Session.Send(leavePacket);
                }

                // 타인한테 정보 전송
                {
                    // 다른 플레이어가 나갔다면 DESPWAN을 해줘야 한다.
                    S_Despawn despawnPacket = new S_Despawn();
                    despawnPacket.PlayerIds.Add(player.Info.PlayerId);
                    foreach (Player p in _players)
                    {
                        if (p != player)
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
                PlayerInfo info = player.Info;
                info.PosInfo = movePacket.PosInfo;

                // Broadcast
                S_Move resMovePacket = new S_Move();
                resMovePacket.PlayerId = player.Info.PlayerId;
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
                PlayerInfo info = player.Info;
                if (info.PosInfo.State != CreatureState.Idle)
                    return;

                // Todo : 스킬 사용 가능 여부 체크

                // 통과
                info.PosInfo.State = CreatureState.Skill;

                S_Skill sKill = new S_Skill() { Info = new SkillInfo() };
                sKill.PlayerId = info.PlayerId;
                sKill.Info.SkillId = 1; // 나중에 스킬과 관련된 부분은 데이터 시트(Json, XML로 따로 관리)로 관리 해서 관리
                Broadcast(sKill);

                // Todo : 데미지 판정
            }
        }

        public void Broadcast(IMessage packet)
        {
            // C_MoveHandler를 여러 쓰레드에서 처리를 할 것이기 때문에 lock을 잡아줘야 한다.
            lock (_lock)
            {
                foreach (Player p in _players)
                {
                    p.Session.Send(packet);
                }
            }
        }
    }
}
