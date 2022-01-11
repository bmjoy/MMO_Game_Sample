using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;

namespace Server.Game
{
    public partial class GameRoom : JobSerializer
    {
        public void HandleMove(Player player, C_Move movePacket)
        {
            if (player == null)
                return;

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

        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            // 나중에는 player가 해당 Room에 실제로 존재하는지 더블 체크 하는 것도 좋다
            if (player == null)
                return;
            ObjectInfo info = player.Info;
            if (info.PosInfo.State != CreatureState.Idle)
                return;

            // 통과
            info.PosInfo.State = CreatureState.Skill;

            S_Skill skill = new S_Skill() { Info = new SkillInfo() };
            skill.ObjectId = info.ObjectId;
            skill.Info.SkillId = skillPacket.Info.SkillId; // 나중에 스킬과 관련된 부분은 데이터 시트(Json, XML로 따로 관리)로 관리 해서 관리
            Broadcast(skill); // 스킬을 사용한다는 애니메이션을 맞추기 위한 Broadcast

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
                            Console.WriteLine("Hit GameObject !");
                        }
                    }
                    break;

                // 투사체 스킬의 종류에 따라서 분기 처리를 해주는 작업을 해줘야 한다.
                case SkillType.SkillProjectile:
                    {
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
                        Push(EnterGame, arrow);
                    }
                    break;
            }
        }

    }
}
