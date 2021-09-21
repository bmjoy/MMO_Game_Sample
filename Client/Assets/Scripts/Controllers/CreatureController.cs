﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class CreatureController : MonoBehaviour
{
    public float _speed = 5.0f;
	public Vector3Int CellPos { get; set; } = Vector3Int.zero;
	protected Animator _animator;
    protected SpriteRenderer _sprite;
    protected CreatureState _state = CreatureState.Idle;
    public CreatureState State
    {
        get { return _state; }
        set 
        {
            if (_state == value)
                return;
            _state = value;
            UpdateAnimation();
        }
    }

    // 내가 방금전 바라보던 방향
    protected MoveDir _lastDir = MoveDir.Down;

	protected MoveDir _dir = MoveDir.Down;
	public Vector3Int GetFrontCellPos()
	{
		Vector3Int cellPos = CellPos;
		switch (_lastDir)
		{
			case MoveDir.Up:
				cellPos += Vector3Int.up;
				break;
			case MoveDir.Down:
				cellPos += Vector3Int.down;
				break;
			case MoveDir.Left:
				cellPos += Vector3Int.left;
				break;
			case MoveDir.Right:
				cellPos += Vector3Int.right;
				break;
		}
		return cellPos;
	}
	public MoveDir Dir
	{
		get { return _dir; }
		set 
		{
			if (_dir == value)
				return;

			_dir = value;
            if (value != MoveDir.None)
                _lastDir = value;
            UpdateAnimation();
		}
	}

    protected virtual void UpdateAnimation()
    {
        // 캐릭터의 현재 상태가 Idle일 때 이전에 바라보던 방향으로 애니메이션 실행
        if (_state == CreatureState.Idle)
        {
            switch (_lastDir)
            {
                case MoveDir.Up:
                    _animator.Play("IDLE_BACK");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Down:
                    _animator.Play("IDLE_FRONT");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Left:
                    _animator.Play("IDLE_RIGHT");
                    _sprite.flipX = true;
                    break;
                case MoveDir.Right:
                    _animator.Play("IDLE_RIGHT");
                    _sprite.flipX = false;
                    break;
            }
        }
        else if (_state == CreatureState.Moving)
        {
			switch (_dir)
			{
				case MoveDir.Up:
					_animator.Play("WALK_BACK");
                    _sprite.flipX = false;
					break;
				case MoveDir.Down:
					_animator.Play("WALK_FRONT");
                    _sprite.flipX = false;
					break;
				case MoveDir.Left:
					_animator.Play("WALK_RIGHT");
                    _sprite.flipX = true;
					break;
				case MoveDir.Right:
					_animator.Play("WALK_RIGHT");
                    _sprite.flipX = false;
					break;
			}
        }
        else if (_state == CreatureState.Skill)
        {
            // Todo
			switch (_lastDir)
			{
				case MoveDir.Up:
					_animator.Play("ATTACK_BACK");
                    _sprite.flipX = false;
					break;
				case MoveDir.Down:
					_animator.Play("ATTACK_FRONT");
                    _sprite.flipX = false;
					break;
				case MoveDir.Left:
					_animator.Play("ATTACK_RIGHT");
                    _sprite.flipX = true;
					break;
				case MoveDir.Right:
					_animator.Play("ATTACK_RIGHT");
                    _sprite.flipX = false;
					break;
			}
        }
        else
        {

        }
    }

	void Start()
    {
        Init();
	}

    void Update()
    {
        UpdateController();
	}

    protected virtual void Init()
    {
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
		Vector3 pos = Managers.Map.CurrentGrid.CellToWorld(CellPos) + new Vector3(0.5f, 0.5f);
		transform.position = pos;
    }

    protected virtual void UpdateController()
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
				break;
			case CreatureState.Dead:
				break;
		}
    }
	// 이동 가능한 상태일 때, 실제 좌표를 이동한다
	protected virtual void UpdateIdle()
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
				if (Managers.Object.Find(destPos) == null)
				{
					CellPos = destPos;
				}
			}
		}
	}

    protected virtual void UpdateMoving()
	{
		Vector3 destPos = Managers.Map.CurrentGrid.CellToWorld(CellPos) + new Vector3(0.5f, 0.5f);
		Vector3 moveDir = destPos - transform.position;

		// 도착 여부 체크
		float dist = moveDir.magnitude;
		if (dist < _speed * Time.deltaTime)
		{
			transform.position = destPos;
            // 예외적으로 애니메이션을 직접 컨트롤
			_state = CreatureState.Idle;
            if (_dir == MoveDir.None)
                UpdateAnimation();
		}
		else
		{
			transform.position += moveDir.normalized * _speed * Time.deltaTime;
			State = CreatureState.Moving;
		}
	}

	protected virtual void UpdateSkill()
	{

	}
	protected virtual void UpdateDead()
	{

	}
	public virtual void OnDamaged()
	{
		
	}
}
