using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class MonsterController : CreatureController
{
	Coroutine _coSkill;

	// [SerializeField]
	// bool _rangedSkill = false;

	protected override void Init()
	{
		base.Init();
	}

	protected override void UpdateIdle()
	{
		base.UpdateIdle();
	}

	public override void OnDamaged()
	{
		//Managers.Object.Remove(Id);
		//Managers.Resource.Destroy(gameObject);
	}

	public override void UseSkill(int skillId)
	{
		if (skillId == 1)
		{
			// 서버 쪽에서 허락을 받고 스킬을 발사해준다.
			State = CreatureState.Skill;
		}
	}
}
