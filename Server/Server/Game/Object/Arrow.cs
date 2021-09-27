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
            if (Owner == null || Room == null)
                return;

            if (_nextMoveTick >= Environment.TickCount64)
                return;

            // 50이 내가 원하는 시간
            _nextMoveTick = Environment.TickCount64 + 50;

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
                    // Todo : 피격 판정
                }

                // 소멸
                Room.LeaveGame(Id);
            }
        }
    }
}
