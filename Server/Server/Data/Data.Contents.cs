using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Data
{
	#region Stat
	
	[Serializable]
	public class StatData : ILoader<int, StatInfo>
	{
		public List<StatInfo> stats = new List<StatInfo>();

		public Dictionary<int, StatInfo> MakeDict()
		{
			Dictionary<int, StatInfo> dict = new Dictionary<int, StatInfo>();
			foreach (StatInfo stat in stats)
            {
				stat.Hp = stat.MaxHp;
				dict.Add(stat.Level, stat);
			}
			return dict;
		}
	}
    #endregion

    #region Skill

    [Serializable]
    public class Skill
    {
		public int id;
		public string name;
		public float cooldown;
		public int damage;
		public SkillType skllType;
		public ProjectileInfo projectile;
	}

    public class ProjectileInfo
    {
		public string name;
		public float speed;
		public int range;
		public string prefab;
	}

	[Serializable]
	public class SkillData : ILoader<int, Skill>
	{
		public List<Skill> skills = new List<Skill>();

		public Dictionary<int, Skill> MakeDict()
		{
			Dictionary<int, Skill> dict = new Dictionary<int, Skill>();
			foreach (Skill skill in skills)
				dict.Add(skill.id, skill);
			return dict;
		}
	}
	#endregion

	#region Item 

    [Serializable]
    public class ItemData
    {
		public int id;
		// 나중에 다국어로 서비스를 해야하는 경우를 고려하면
		// 이렇게 string 값을 바로 넣는 것이 아니라 별도의 id 값을 들고 있어서
		// 해당 나라의 언어로 변경해줘야 한다.
		public string name;
		public ItemType itemType;

		// 무기에서는 공격력, 방어구에서는 방어력, 컴슈머블에서는 최대 수량
		// 각각의 데이터를 여기에 때려 박는 것이 첫 번째 방법이다.
		// 하지만 이렇게 되면 혼돈이다.
		// 따라서 따로 분리하는 방법이 좋다.
		// int damage;
		// int defence;
	}

	public class WeaponData : ItemData
	{
		public WeaponType weaponType;
		public int damage;
	}

	public class ArmorData : ItemData
	{
		public ArmorTpye armorTpye;
		public int defence;
	}

	public class ConsumableData : ItemData
	{
		public ConsumableType consumableType;
		public int maxCount;
	}

	// Loader를 각 별도로 만들면 어떨까하는 생각이 들지만
	// 나중에 가면 어떤 몬스터가 죽을 때 특정 아이템을 떨구게 만들어 줄 것이다.
	// 이때 template ID가 100번일 때 해당 아이템이 무기인지 방어구인지 
	// 알 방법이 없게 된다.
	// 따라서 ItemData라는 하나의 Dictionary에서 관리하는 것이 좋다.
	[Serializable]
	public class ItemLoader : ILoader<int, ItemData>
	{
		public List<WeaponData> weapons = new List<WeaponData>();
		public List<ArmorData> armors = new List<ArmorData>();
		public List<ConsumableData> consumables = new List<ConsumableData>();
		
		// template id를 알면 해당 아이템을 추출할 수 있도록 만들어보자.
		public Dictionary<int, ItemData> MakeDict()
		{
			Dictionary<int, ItemData> dict = new Dictionary<int, ItemData>();
			foreach (ItemData item in weapons)
			{
				item.itemType = ItemType.Weapon;
				dict.Add(item.id, item);
			}
			foreach (ItemData item in armors)
			{
				item.itemType = ItemType.Armor;
				dict.Add(item.id, item);
			}
			foreach (ItemData item in consumables)
			{
				item.itemType = ItemType.Consumable;
				dict.Add(item.id, item);
			}
			return dict;
		}
	}
	#endregion
}
