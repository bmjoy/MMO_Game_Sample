using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class Arrow : Projectile
    {
        public GameObject Owner { get; set; }

        public override void Update()
        {
            if (Data == null || Data.projectile == null || Owner == null || Room == null)
                return;
            // 1000 ms => tick은 ms 단위로 계산되기 때문에 1초를 speed로 나누면 내가 기다려야 하는 시간이 계산이 된다
            int tick = (int)(1000 / Data.projectile.speed);

            // 해당 tick 이후에 실행을 해달라고 예약을 거는 방식으로 수정
            Room.PushAfter(tick, Update);
            
            // 앞으로 이동하는 연산
            Vector2Int destPos = GetFrontCellPos(); 
            if (Room.Map.CanGo(destPos))
            {
                CellPos = destPos;

                S_Move movePacket = new S_Move();
                movePacket.ObjectId = Id;
                movePacket.PosInfo = PosInfo;
                Room.Broadcast(movePacket);

                Console.WriteLine("Move Arrow");
            }
            else
            {
                // 목적지에 대상이 있다면
                GameObject target = Room.Map.Find(destPos);

                if (target != null)
                {
                    // 공격력 = damage + 스탯
                    target.OnDamaged(this, Data.damage + Owner.TotalAttack);
                }

                // 소멸
                Room.Push(Room.LeaveGame, Id);
            }
        }

        public override GameObject GetOwner()
		{
			return Owner;
		}
    }
}
