using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class Arrow : Projectile
    {
        public GameObject Owner { get; set; }

        long _nextMoveTick = 0;
        public override void Update()
        {
            if (Data == null || Data.projectile == null || Owner == null || Room == null)
                return;

            if (_nextMoveTick >= Environment.TickCount64)
                return;

            // 1000 ms => tick은 ms 단위로 계산되기 때문에 1초를 speed로 나누면 내가 기다려야 하는 시간이 계산이 된다
            long tick = (long)(1000 / Data.projectile.speed);
            _nextMoveTick = Environment.TickCount64 + tick;

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
                    target.OnDamaged(this, Data.damage + Owner.Stat.Attack);
                }

                // 소멸
                Room.LeaveGame(Id);
            }
        }
    }
}
