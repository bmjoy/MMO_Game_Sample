using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;
using static Define;

public class MyPlayerController : PlayerController
{
	bool _moveKeyPressed = false;
	public int WeaponDamage { get; private set; }
	public int ArmorDefence { get; private set; }

	protected override void Init()
	{
		base.Init();
		RefreshAdditionalStat();
	}
    
    protected override void UpdateController()
	{
		GetUIKeyInput();
		switch (State)
		{
			case CreatureState.Idle:
				GetDirInput();
				break;
			case CreatureState.Moving:
				GetDirInput();
				break;
		}
		
		base.UpdateController();
	}

    protected override void UpdateIdle()
	{
		// 이동 상태로 갈지 확인
		if (_moveKeyPressed)
		{
			State = CreatureState.Moving;
			return;
		}

		// 스킬 상태로 갈지 확인
		if (_coSkillCoolTime == null && Input.GetKey(KeyCode.Space))
		{
			Debug.Log("Skill");

			// 쿨타임 관리
			C_Skill skill = new C_Skill() { Info = new SkillInfo() };
			skill.Info.SkillId = 2;
			Managers.Network.Send(skill);
			_coSkillCoolTime = StartCoroutine("CoInputCoolTime", 0.2f);
		}
	}

	Coroutine _coSkillCoolTime;
	IEnumerator CoInputCoolTime(float time)
	{
		yield return new WaitForSeconds(time);
		_coSkillCoolTime = null;
	}

    void LateUpdate()
	{
		Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
	}
	
	void GetUIKeyInput()
	{
		if (Input.GetKeyDown(KeyCode.I))
		{
			UI_GameScene gameSceneUI = Managers.UI.SceneUI as UI_GameScene;
			UI_Inventory invenUI = gameSceneUI.InvenUI;

			if (invenUI.gameObject.activeSelf)
			{
				invenUI.gameObject.SetActive(false);
			}
			else
			{
				invenUI.gameObject.SetActive(true);
				invenUI.RefreshUI();
			}
		}
		else if (Input.GetKeyDown(KeyCode.C))
		{
			UI_GameScene gameSceneUI = Managers.UI.SceneUI as UI_GameScene;
			UI_Stat statUI = gameSceneUI.StatUI;

			if (statUI.gameObject.activeSelf)
			{
				statUI.gameObject.SetActive(false);
			}
			else
			{
				statUI.gameObject.SetActive(true);
				statUI.RefreshUI();
			}
		}
	}

    void GetDirInput()
	{
		_moveKeyPressed  = true;
		if (Input.GetKey(KeyCode.W))
		{
			Dir = MoveDir.Up;
		}
		else if (Input.GetKey(KeyCode.S))
		{
			Dir = MoveDir.Down;
		}
		else if (Input.GetKey(KeyCode.A))
		{
			Dir = MoveDir.Left;
		}
		else if (Input.GetKey(KeyCode.D))
		{
			Dir = MoveDir.Right;
		}
		else
		{
			_moveKeyPressed = false;
		}
	}

	protected override void MoveToNextPos()
	{
		if (_moveKeyPressed == false)
		{
			State = CreatureState.Idle;
			CheckUpdatedFlag();
			return;
		}

		Vector3Int destPos = CellPos;

		switch (Dir)
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

		if (Managers.Map.CanGo(destPos))
		{
			if (Managers.Object.FindCreature(destPos) == null)
			{
				CellPos = destPos;
			}
		}

		CheckUpdatedFlag();
	}


    

	protected override void CheckUpdatedFlag()
	{
		if (_updated)
		{
			C_Move movePacket = new C_Move();
			movePacket.PosInfo = PosInfo;
			Managers.Network.Send(movePacket);
			_updated = false;
		}
	}
	
	public void RefreshAdditionalStat()
	{   
		// 데미지 판정은 서버에서 하는데
		WeaponDamage = 0;
		ArmorDefence = 0;

		foreach (Item item in Managers.Inven.Items.Values)
		{
			// 현재 아이템이 착용 중이 아니라면 스킵
			if (item.Equipped == false)
				continue;
			switch (item.ItemType)
			{
				case ItemType.Weapon:
					WeaponDamage += ((Weapon)item).Damage;
					break;
				case ItemType.Armor:
					ArmorDefence += ((Armor)item).Defence;
					break;
			}
		}
	}
}