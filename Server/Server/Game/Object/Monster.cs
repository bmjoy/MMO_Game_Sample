using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;

namespace Server.Game
{
    public class Monster : GameObject
    {
        public int TemplateId { get; private set; }
        public Monster()
        {
            ObjectType = GameObjectType.Monster;
        }

        public void Init(int templateId)
        {
            TemplateId = templateId;

            MonsterData monsterData = null;
            DataManager.MonsterDict.TryGetValue(TemplateId, out monsterData);
            Stat.MergeFrom(monsterData.stat);
            Stat.Hp = monsterData.stat.MaxHp;
            State = CreatureState.Idle;
        }

        IJob _job;

        // FSM (Finite State Machine)
        public override void Update()
        {
            switch (State)
            {
                case CreatureState.Idle:
                    UpdateIdle();
                    break;
                case CreatureState.Moving:
                    UpdateMoving();
                    break;
                case CreatureState.Skill:
                    UpdateSkill();
                    break;
                case CreatureState.Dead:
                    UpdateDead();
                    break;
            }

            // 5프레임으로 (0.2초마다 한번씩 업데이트)
            if (Room != null)
                _job = Room.PushAfter(200, Update);
        }

        // 이렇게 참조로 저장을 해도 되고 id를 통해 매 틱마다 찾아도 된다.
        Player _target;
        int _searchCellDist = 10;
        int _chaseCellDist = 20;

        long _nextSearchTick = 0;

        // Idle의 상태일 때 => 주변에 플레이어가 있는지 찾아보고 추척
        protected virtual void UpdateIdle()
        {
            // Player의 위치가 몬스터의 위치랑 비교적 비슷한 곳에 있는지 여부를 체크
            Player target = Room.FindPlayer(p =>
            {
                Vector2Int dir = p.CellPos - CellPos;
                return dir.cellDistFromZero <= _searchCellDist;
            });


            if (target == null)
                return;

            _target = target;
            State = CreatureState.Moving;
        }

        int _skillRange = 1;
        long _nextMoveTick = 0;

        protected virtual void UpdateMoving()
        {
            if (_nextMoveTick > Environment.TickCount64)
                return;
            // 스피드란 1초 동안 몇 칸을 움직일 수 있냐는 개념
            // 일단 속도가 빨라질 수록 Tick이 더 빨리 돌아야한다는 개념 정도로 이해해보자
            int moveTick = (int)(1000 / Speed);
            _nextMoveTick = Environment.TickCount64 + moveTick;

            // 내가 쫓고 있는 플레이어가 다른 방으로 가게 되었을 경우
            if (_target == null || _target.Room != Room)
            {
                _target = null;
                State = CreatureState.Idle;
                BroadcastMove();
                return;
            }
            Vector2Int dir = _target.CellPos - CellPos;
            int dist = dir.cellDistFromZero;

            // 범위를 벗어났을 경우 => 플레이어가 너무 빨리 도망 갔을 경우
            if (dist == 0 || dist > _chaseCellDist)
            {
                _target = null;
                State = CreatureState.Idle;
                BroadcastMove();
                return;
            }

            // checkObjects: false => 길을 찾을 때 주변에 플레이어나 몬스터는 무시하고 찾는 방식을 사용
            List<Vector2Int> path = Room.Map.FindPath(CellPos, _target.CellPos, checkObjects: true);

            // 갈 수 있는 길이 없다 || 너무 멀리 떨어져 있다.
            if (path.Count < 2 || path.Count > _chaseCellDist)
            {
                _target = null;
                State = CreatureState.Idle;
                BroadcastMove();
                return;
            }

            // 스킬로 넘어갈지 체크
            // 스킬 사정거리 안에 들어왔고 
            // dir.x == 0 || dir.y == 0 동서남북으로만 스킬 사용 => x, y가 1이면 대각선이기 때문
            if (dist <= _skillRange && (dir.x == 0 || dir.y == 0))
            {
                _coolTick = 0;
                State = CreatureState.Skill;
                return;
            }

            // 목적지 - 현재 위치 => 방향 
            Dir = GetDirFromVec(path[1] - CellPos);

            // 여기까지 왔으면 실제로 이동이 가능
            // 이동을 할 때 바로 CellPos를 변경해서는 안되고 Map에 있는 Grid도 갱신을 해줘야 한다.
            Room.Map.ApplyMove(this, path[1]);
            BroadcastMove();
        }

        void BroadcastMove()
        {
            // 다른 플레이어한테도 알려준다
            S_Move movePacket = new S_Move();
            movePacket.ObjectId = Id;
            movePacket.PosInfo = PosInfo;
            Room.Broadcast(CellPos, movePacket);
        }

        // 피격 판정, 연속적으로 스킬 사용 => 쿨 계산
        long _coolTick = 0;
        protected virtual void UpdateSkill()
        {
            // 지금 바로 공격 가능
            if (_coolTick == 0)
            {
                // 유효한 타겟인지
				if (_target == null || _target.Room != Room)
                {
                    _target = null;
                    State = CreatureState.Moving;
                    BroadcastMove();
                    return;
                }

                // 스킬이 아직 사용 가능한지
                Vector2Int dir = (_target.CellPos - CellPos);
                int dist = dir.cellDistFromZero;
                bool canUseSkill = (dist <= _skillRange && (dir.x == 0 || dir.y == 0));
                if (canUseSkill == false)
                {
                    State = CreatureState.Moving;
                    BroadcastMove();
                    return;
                }

                // 타겟팅 방향 주시
                MoveDir lookDir = GetDirFromVec(dir);
                if (Dir != lookDir)
                {
                    Dir = lookDir;
                    BroadcastMove();
                }

                // 지금은 1이라고 하드코딩 했지만 나중에는 몬스터 데이터 시트를 따로 빼서 해당 몬스터의 AI 스킬을 연동해줘야 한다.
                Skill skillData = null;
                DataManager.SkillDict.TryGetValue(1, out skillData);

                // 데미지 판정
                _target.OnDamaged(this, skillData.damage + TotalAttack);

                // 스킬 사용 Broadcast
                S_Skill skill = new S_Skill() { Info = new SkillInfo() };
                skill.ObjectId = Id;
                skill.Info.SkillId = skillData.id;
                Room.Broadcast(CellPos, skill);

                // 스킬 쿨타임 적용
                int coolTick = (int)(1000 * skillData.cooldown);
                _coolTick = Environment.TickCount64 + coolTick;
            }

            // 다음에 스킬을 쓸 수 있는지 여부를 체크
            if (_coolTick > Environment.TickCount64)
                return;

            // coolTick만큼의 시간이 지나서 스킬을 사용할 준비가 되었음
            _coolTick = 0;
        }

        protected virtual void UpdateDead()
        {

        }

        public override void OnDead(GameObject attacker)
        {
            // 몬스터가 죽은 다음에 이후에 처리해야할 일감을 취소해주는 방식
            if (_job != null)
            {
                _job.Cancle = true;
                _job = null;
            }

            base.OnDead(attacker);

            GameObject owner = attacker.GetOwner();
            if (owner.ObjectType == GameObjectType.Player)
            {
                RewardData rewardData = GetRandomReward();
                if (rewardData != null)
                {
                    Player player = (Player)owner;
                    DbTransaction.RewardPlayer(player, rewardData, Room);
                    // Item.MakeItem();
                    // player.Inven.Add();
                }
            }
        }

        RewardData GetRandomReward()
        {
            MonsterData monsterData = null;
            DataManager.MonsterDict.TryGetValue(TemplateId, out monsterData);
            
            int rand = new Random().Next(0, 101);

            // rand = 0 ~ 100 -> 42
            // 10 10 10 10 10
            int sum = 0;
            foreach (RewardData rewardData in monsterData.rewards)
            {
                sum += rewardData.probability;
                if (rand <= sum)
                {
                    return rewardData;
                }
            }

            return null;
        }
    }
}