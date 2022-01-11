using Google.Protobuf.Protocol;
using Server.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class Player : GameObject
    {
        public int PlayerDbId { get; set; }
        public ClientSession Session { get; set; }
        public Inventory Inven { get; private set; } = new Inventory();
        public int WeaponDamage { get; private set; }
        public int ArmorDefence { get; private set; }

        public override int TotalAttack { get { return Stat.Attack + WeaponDamage; } }
		public override int TotalDefence { get { return ArmorDefence; } }

        public Player()
        {
            ObjectType = GameObjectType.Player;
        }

        public override void OnDamaged(GameObject attacker, int damage)
        {
            base.OnDamaged(attacker, damage);
        }


        // PVP 때 사용
        public override void OnDead(GameObject attacker)
        {
            base.OnDead(attacker);
        }

        public void OnLeaveGame()
        {
            // Todo
            // DB 연동?
            // 1) 피가 깍일 대마다 DB에 접근할 필요가 있을까?
            //  => 방에 나갔을 때 처리하는 방식으로 수정

            // 하지만 이렇게 만들면 인디게임 수준에서는 괜찮지만
            // 진지하게 게임을 만들 때는 문제가 된다.
            // 1) 서버가 다운되면 아직 저장되지 않은 정보가 날라간다.
            //   - 즉 현재는 나갈 때만 저장을 하는 구조이기 때문에
            //   - 나가기 전 서버가 다운이 되면 정보가 저장되지 않는다.
            // 2) 코드 흐름을 다 막아버린다!!
            //   - 즉 현재 OnLeaveGame을 호출 하는 주체는 GameRoom에서 해당 Player가 나갔을 때 호출을 하고 있는데
            //   - 현재 GameRoom의 경우 JobSerializer를 상속 받아서 여러 쓰레드가 GameRoom에서 해야할 처리를 도맡아
            //   - 진행을 하고 있는데 이렇게 DB에 접근을 하는 작업을 한명이 하게 된다면 lock에 걸려서 병목 현상이 일어나게 된다.
            //   - 이렇게 MMO에서 전투로직이 실행되는 쪽에서 바로 DB 연동을 실행하는 부분은 매우 위험한 일이다.
            // 해결 방법
            // - 비동기(Async) 방법 사용?
            // - 다른 쓰레드로 DB 일감을 던져버리면 되지 않을까?
            //  => 결과를 받아서 이어서 처리를 해야하는 경우가 많음
            // ex) 아이템과 관련된 작업
            //  => 몬스터를 잡아서 필드 아이템 드랍, 아이템 강화 결과를 보고 싶을 경우
            //  => DB에 저장되지 않은 상태에서(즉, 인벤토리에 저장하지 않은 상태) 이후 작업을 하는 것은 매우 문제가 있다.
            // 따라서 다른 쓰레드에서 일감을 던지고 그후에 완료 되었다는 통보를 받은 후 작업을 진행해야한다.
            
            DbTransaction.SavePlayerStatus_Step1(this, Room);
        }

        public void HandleEquipItem(C_EquipItem equipPacket)
        {
            Item item = Inven.Get(equipPacket.ItemDbId);
            if (item == null)
                return;
            if (item.ItemType == ItemType.Consumable)
                return;

            // 착용 요청이라면, 겹치는 부위는 해제
            if (equipPacket.Equipped)
            {
                // 겹치는 부위가 있어서 해지해야할 아이템이 있는지 찾아보자
                Item unequipItem = null;
                if (item.ItemType == ItemType.Weapon)
                {
                    // 현재 인벤토리에서 장착 중인 아이템 중 무기를 가지고 온다.
                    // => 현재 장착 중인 아이템을 해지하기 위해서
                    unequipItem = Inven.Find(i => i.Equipped && i.ItemType == ItemType.Weapon);
                }
                else if (item.ItemType == ItemType.Armor)
                {
                    // 해당 아이템이 어떤 Armor 타입인지 확인
                    ArmorType armorType = ((Armor)item).ArmorType;
                    // 현재 장착 중인 아이템 중, 
                    // 아이템 타입이 Armor이고
                    // 해당 아이템의 Armor 타입과 동일한 타입의 Armor를 가지고 온다.
                    unequipItem = Inven.Find(
                        i => i.Equipped 
                        && i.ItemType == ItemType.Armor
                        && ((Armor)i).ArmorType == armorType);
                }

                // 해지 해야할 아이템 정보를 클라에게 발송
                if (unequipItem != null)
                {
                    unequipItem.Equipped = false;
                    DbTransaction.EquipItemNoti(this, unequipItem); 

                    // 클라에 통보
                    S_EquipItem equipOkItem = new S_EquipItem();
                    equipOkItem.ItemDbId = unequipItem.ItemDbId;
                    equipOkItem.Equipped = unequipItem.Equipped;
                    Session.Send(equipOkItem); 
                }
            }

            // 새로운 아이템을 장착 하는 부분
            {
                // DB 연동
                // 메모리 선 적용
                item.Equipped = equipPacket.Equipped;

                // DB에 Noti
                // 이렇게 Noti만 날리고 잊고 살아도 된다.
                DbTransaction.EquipItemNoti(this, item); 

                // 클라에 통보
                S_EquipItem equipOkItem = new S_EquipItem();
                equipOkItem.ItemDbId = equipPacket.ItemDbId;
                equipOkItem.Equipped = equipPacket.Equipped;
                Session.Send(equipOkItem); 
            }
            RefreshAdditionalStat();
        }
        
        public void RefreshAdditionalStat()
        {   
            // 변경이 필요한 Stat의 정보만 받아와서 처리하기 보다 그냥 스탯을 다시 계산을 하는 것이 속편하다.
            // 성능상 손해를 조금 보기는 하지만 버그를 줄일 수 있는 방식이기 때문
            WeaponDamage = 0;
            ArmorDefence = 0;

            foreach (Item item in Inven.Items.Values)
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
}
