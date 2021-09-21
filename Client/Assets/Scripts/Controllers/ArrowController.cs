using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class ArrowController : CreatureController
{
    protected override void Init()
    {
        // Todo
		switch (_lastDir)
		{
			case MoveDir.Up:
				transform.rotation = Quaternion.Euler(0, 0, 0);
				break;
			case MoveDir.Down:
				transform.rotation = Quaternion.Euler(0, 0, -180);
				break;
			case MoveDir.Left:
				transform.rotation = Quaternion.Euler(0, 0, 90);
				break;
			case MoveDir.Right:
				transform.rotation = Quaternion.Euler(0, 0, -90);
				break;
		}
        base.Init();
    }

    protected override void UpdateAnimation()
    {

    }

    // 이동 가능한 상태일 때, 실제 좌표를 이동한다
	protected override void UpdateIdle()
	{
		if (_dir != MoveDir.None)
		{
			Vector3Int destPos = CellPos;
			switch (_dir)
			{
				case MoveDir.Up:
					destPos += Vector3Int.up;
					break;
				case MoveDir.Down:
					destPos += Vector3Int.down;
					break;
				case MoveDir.Left:
					destPos += Vector3Int.left;
					break;
				case MoveDir.Right:
					destPos += Vector3Int.right;
					break;
			}
			State = CreatureState.Moving;
			if (Managers.Map.CanGo(destPos))
			{
                GameObject go = Managers.Object.Find(destPos);
                // 목적지에 go가 없다면 한칸 이동
				if (go == null)
				{
					CellPos = destPos;
				}
                // 있다면
                else
                {
					CreatureController cc = go.GetComponent<CreatureController>();
					if (cc != null)
					{
						cc.OnDamaged();
					}
                    Managers.Resource.Destroy(gameObject); 
                }
			}
            // 갈수 없다면 피격된 상태이므로 파괴
            else
            {
                Managers.Resource.Destroy(gameObject);
            }
		}
	}
}
