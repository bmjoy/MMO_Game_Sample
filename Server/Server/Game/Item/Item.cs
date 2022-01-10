using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;

namespace Server.Game
{
    public class Item
    {
        public  ItemInfo Info { get; } = new ItemInfo();
        public int ItemDbId 
        {
            get { return Info.ItemDbId; }
            set { Info.ItemDbId = value; }
        } 
        public int TemplateId 
        {
            get { return Info.TemplateId; }
            set { Info.TemplateId = value; }
        } 
        public int Count 
        {
            get { return Info.Count; }
            set { Info.Count = value; }
        } 
        public int Slot
        {
            get { return Info.Slot; }
            set { Info.Slot = value; }
        }

        public ItemType ItemType { get; private set; }

        // 이 아이템이 겹쳐지는 여부를 판단
        // 일종의 캐싱의 용도
        // TemplateId를 알면 DataManager에 접근해서 해당하는 Max count를 보는 식으로 
        // 겹쳐지는 여부를 알 수가 있다.
        // 마찬가지로 아이템 타입만 알면 Stackable인지 어느정도 확인할 수가 있다.
        public bool Stackable { get; protected set; }

        public Item(ItemType itemType)
        {
            ItemType = ItemType;
        }

        public static Item MakeItem(ItemDb itemDb)
        {
            Item item = null;

            ItemData itemData = null;
            DataManager.ItemDict.TryGetValue(itemDb.TemplateId, out itemData);
            
            if (itemData == null)
                return null;
            
            switch (itemData.itemType)
            {
                case ItemType.Weapon:
                    item = new Weapon(itemDb.TemplateId);
                    break;
                case ItemType.Armor:
                    item = new Armor(itemDb.TemplateId);
                    break;
                case ItemType.Consumable:
                    item = new Consumable(itemDb.TemplateId);
                    break;
            }

            if (item != null)
            {
                item.ItemDbId = itemDb.ItemDbId;
                item.Count = itemDb.Count;
                item.Slot = itemDb.Slot;
            }

            return item;
        }
    }

    public class Weapon : Item
    {
        public WeaponType WeaponType { get; private set; }
        public int Damage { get; private set; }

        // TemplateId에 따라 WeaponType과 Damage를 채워줘야 한다.
        public Weapon(int templateId) : base(ItemType.Weapon)
        {
            Init(templateId);
        }

        void Init(int templateId)
        {
            ItemData itemData = null;
            DataManager.ItemDict.TryGetValue(templateId, out itemData);
            if (itemData.itemType != ItemType.Weapon)
                return;
            
            WeaponData data = (WeaponData)itemData;
            {
                TemplateId = data.id;
                // 무기는 겹치는 개념이 아니기 때문
                Count = 1;
                WeaponType = data.weaponType;
                Damage = data.damage;
                Stackable = false;
            }
        }
    }

    public class Armor : Item
    {
        public ArmorType ArmorType { get; private set; }
        public int Defence { get; private set; }

        // TemplateId에 따라 WeaponType과 Damage를 채워줘야 한다.
        public Armor(int templateId) : base(ItemType.Armor)
        {
            Init(templateId);
        }

        void Init(int templateId)
        {
            ItemData itemData = null;
            DataManager.ItemDict.TryGetValue(templateId, out itemData);
            if (itemData.itemType != ItemType.Armor)
                return;
            
            ArmorData data = (ArmorData)itemData;
            {
                TemplateId = data.id;
                // 무기는 겹치는 개념이 아니기 때문
                Count = 1;
                ArmorType = data.armorType;
                Defence = data.defence;
                Stackable = false;
            }
        }
    }

    public class Consumable : Item
    {
        public ConsumableType ConsumableType { get; private set; }
        public int MaxCount { get; private set; }

        // TemplateId에 따라 WeaponType과 Damage를 채워줘야 한다.
        public Consumable(int templateId) : base(ItemType.Consumable)
        {
            Init(templateId);
        }

        void Init(int templateId)
        {
            ItemData itemData = null;
            DataManager.ItemDict.TryGetValue(templateId, out itemData);
            if (itemData.itemType != ItemType.Consumable)
                return;
            
            ConsumableData data = (ConsumableData)itemData;
            {
                TemplateId = data.id;
                // 무기는 겹치는 개념이 아니기 때문
                Count = 1;
                MaxCount = data.maxCount;
                ConsumableType = data.consumableType;
                Stackable = (data.maxCount > 1);
            }
        }
    }
}