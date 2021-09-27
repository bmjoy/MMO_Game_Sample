﻿using System;
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

	public static GameObjectType GetObjectTypeById (int id)
	{
		int type = (id >> 24) & 0x7F;
		return (GameObjectType)type;
	}

	public void Add(ObjectInfo info, bool myPlayer = false)
	{
		GameObjectType objectType = GetObjectTypeById(info.ObjectId);
		if (objectType == GameObjectType.Player)
		{
			if (myPlayer)
			{
				GameObject go = Managers.Resource.Instantiate("Creature/MyPlayer");
				go.name = info.Name;
				_objects.Add(info.ObjectId, go);

				MyPlayer = go.GetComponent<MyPlayerController>();
				MyPlayer.Id = info.ObjectId;
				MyPlayer.PosInfo = info.PosInfo;
				MyPlayer.SyncPos();
			}
			else
			{
				GameObject go = Managers.Resource.Instantiate("Creature/Player");
				go.name = info.Name;
				_objects.Add(info.ObjectId, go);

				PlayerController pc = go.GetComponent<PlayerController>();
				pc.Id = info.ObjectId;
				pc.PosInfo = info.PosInfo;
				pc.SyncPos();
			}
		}
		else if (objectType == GameObjectType.Moster)
		{

		}
		else if (objectType == GameObjectType.Projectile)
		{
			GameObject go = Managers.Resource.Instantiate("Creature/Arrow");
			go.name = "Arrow";
			_objects.Add(info.ObjectId, go);

			ArrowController ac = go.GetComponent<ArrowController>();
			ac.Dir = info.PosInfo.MoveDir;
			ac.CellPos = new Vector3Int(info.PosInfo.PosX, info.PosInfo.PosY, 0);
			ac.SyncPos();
		}
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
		GameObject go = FindById(id);
		if (go == null)
			return;
		_objects.Remove(id);
		Managers.Resource.Destroy(go);
	}

	public GameObject FindById(int id)
	{
		GameObject go = null;
		_objects.TryGetValue(id , out go);
		return go;
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
		foreach (GameObject obj in _objects.Values)
			Managers.Resource.Destroy(obj);
		_objects.Clear();
	}
}
