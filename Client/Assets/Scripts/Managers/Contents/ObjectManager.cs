using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;

// 서버쪽에서 부여 받은 ID로 관리
public class ObjectManager
{
	public MyPlayerController MyPlayer { get; set; }

	// 프로젝트에 따라 달라지는 부분이기는 하지만
	// _objects 하나로 관리하는 경우가 있고, players, monsters 등 여러개로 나눠서 관리하는 경우도 있다.
	Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();
	public void Add(PlayerInfo info, bool myPlayer = false)
	{
		if (myPlayer)
		{
			GameObject go = Managers.Resource.Instantiate("Creature/MyPlayer");
			go.name = info.Name;
			_objects.Add(info.PlayerId, go);

			MyPlayer = go.GetComponent<MyPlayerController>();
			MyPlayer.Id = info.PlayerId;
			MyPlayer.CellPos = new Vector3Int(info.PosX, info.PosY, 0);
		}
		else
		{
			GameObject go = Managers.Resource.Instantiate("Creature/Player");
			go.name = info.Name;
			_objects.Add(info.PlayerId, go);

			PlayerController pc = go.GetComponent<PlayerController>();
			pc.Id = info.PlayerId;
			pc.CellPos = new Vector3Int(info.PosX, info.PosY, 0);
		}
	}
	
	public void Add(int id, GameObject go)
	{
		_objects.Add(id, go);
	}

	public void RemoveMyPlayer()
	{
		if (MyPlayer == null)
			return;
		
		Remove(MyPlayer.Id);
		MyPlayer = null;
	}

	public void Remove(int id)
	{
		_objects.Remove(id);
	}
	public GameObject Find(Vector3Int cellPos)
	{
		foreach (GameObject obj in _objects.Values)
		{
			CreatureController cc = obj.GetComponent<CreatureController>();
			if (cc == null)
				continue;

			if (cc.CellPos == cellPos)
				return obj;
		}

		return null;
	}

	public GameObject Find(Func<GameObject, bool> condition)
	{
		foreach (GameObject obj in _objects.Values)
		{
			if (condition.Invoke(obj))
				return obj;
		}

		return null;
	}

	public void Clear()
	{
		_objects.Clear();
	}
}
