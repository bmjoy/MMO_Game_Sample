using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class GameObject
    {
        public GameObjectType ObjectType { get; protected set; } = GameObjectType.None;
		public int Id
        {
			get { return Info.ObjectId; }
			set { Info.ObjectId = value; }
		}

        public GameRoom Room { get; set; }
        public ObjectInfo Info { get; set; } = new ObjectInfo();
        public PositionInfo PosInfo { get; private set; } = new PositionInfo();
		public StatInfo Stat { get; private set; } = new StatInfo();

		public float Speed
        {
			get { return Stat.Speed; }
			set { Stat.Speed = value; }
        }

        public GameObject()
        {
            Info.PosInfo = PosInfo;
			Info.StatInfo = Stat;
        }

		public Vector2Int CellPos
		{
			get
			{
				return new Vector2Int(PosInfo.PosX, PosInfo.PosY);
			}
			set
			{
				PosInfo.PosX = value.x;
				PosInfo.PosY = value.y;
			}
		}

		// 현재 자신이 이동하고 있는 방향의 앞 셀의 위치값은 전달
		public Vector2Int GetFrontCellPos()
        {
			return GetFrontCellPos(PosInfo.MoveDir);
        }

		public Vector2Int GetFrontCellPos(MoveDir dir)
		{
			Vector2Int cellPos = CellPos;

			switch (dir)
			{
				case MoveDir.Up:
					cellPos += Vector2Int.up;
					break;
				case MoveDir.Down:
					cellPos += Vector2Int.down;
					break;
				case MoveDir.Left:
					cellPos += Vector2Int.left;
					break;
				case MoveDir.Right:
					cellPos += Vector2Int.right;
					break;
			}

			return cellPos;
		}

		public virtual void OnDamaged(GameObject attacker, int damage)
		{
			// Max는 둘중 더 큰 숫자를 넣어준다.
			Stat.Hp = Math.Max(Stat.Hp - damage, 0);

			// TODO 
			S_ChangeHp changePacket = new S_ChangeHp();
			changePacket.ObjectId = Id;
			changePacket.Hp = Stat.Hp;
			Room.Broadcast(changePacket);

			if (Stat.Hp <= 0)
            {
				OnDead(attacker);
			}
		}

		public virtual void OnDead(GameObject attacker)
        {
			S_Die diePacket = new S_Die();
			diePacket.ObjectId = Id;
			diePacket.AttackerId = attacker.Id;
			Room.Broadcast(diePacket);

			// 일반적으로 죽으면 풀피 상태에서 랜덤으로 다시 리스폰 되느 경우도 있을 것이고
			// 해당 방에서 내쫓고 재시작을 해야 다시 들어오는 경우도 있을 것이다.
			GameRoom room = Room;
			room.LeaveGame(Id);

			Stat.Hp = Stat.MaxHp;
			PosInfo.State = CreatureState.Idle;
			PosInfo.MoveDir = MoveDir.Down;
			PosInfo.PosX = 0;
			PosInfo.PosY = 0;

			room.EnterGame(this);
		}
	}
}
