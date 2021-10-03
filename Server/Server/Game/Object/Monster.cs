using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;

namespace Server.Game
{
    class Monster : GameObject
    {
        public Monster()
        {
            ObjectType = GameObjectType.Monster;

            // TEMP
            Stat.Level = 1;
            Stat.Hp = 100;
            Stat.MaxHp = 100;
            Stat.Speed = 5.0f;

            State = CreatureState.Idle;
        }

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
        }

        // 이렇게 참조로 저장을 해도 되고 id를 통해 매 틱마다 찾아도 된다.
        Player _target;
        int _searchCellDist = 10;
        int _chaseCellDist = 20;

        long _nextSearchTick = 0;

        // Idle의 상태일 때 => 주변에 플레이어가 있는지 찾아보고 추척
        protected virtual void UpdateIdle()
        {
            // 1초마다 한번식 체크
            if (_nextSearchTick > Environment.TickCount64)
                return;
            _nextSearchTick = Environment.TickCount64 + 1000;

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
                return;
            }

            int dist = (_target.CellPos - CellPos).cellDistFromZero;

            // 범위를 벗어났을 경우 => 플레이어가 너무 빨리 도망 갔을 경우
            if (dist == 0 || dist > _chaseCellDist)
            {
                _target = null;
                State = CreatureState.Idle;
                return;
            }

            // checkObjects: false => 길을 찾을 때 주변에 플레이어나 몬스터는 무시하고 찾는 방식을 사용
            List<Vector2Int> path = Room.Map.FindPath(CellPos, _target.CellPos, checkObjects: false);

            // 갈 수 있는 길이 없다 || 너무 멀리 떨어져 있다.
            if (path.Count < 2 || path.Count > _chaseCellDist)
            {
                _target = null;
                State = CreatureState.Idle;
                return;
            }

            // 목적지 - 현재 위치 => 방향 
            Dir = GetDirFromVec(path[1] - CellPos);

            // 여기까지 왔으면 실제로 이동이 가능
            // 이동을 할 때 바로 CellPos를 변경해서는 안되고 Map에 있는 Grid도 갱신을 해줘야 한다.
            Room.Map.ApplyMove(this, path[1]);

            // 다른 플레이어한테도 알려준다.
            S_Move movePacket = new S_Move();
            movePacket.ObjectId = Id;
            movePacket.PosInfo = PosInfo;
            Room.Broadcast(movePacket);
        }

        protected virtual void UpdateSkill()
        {

        }

        protected virtual void UpdateDead()
        {

        }
    }
}