using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Game
{
    // 진짜 아이템 인벤토리만 해당할지는 나중에
    // 착용 아이템, 보관 아이템 등을 똑같이 관리 가능하기 때문

    // lock이 필요하지 않을까?
    // => Inventory class를 어디서 사용할지가 관건 
    // => 실제로는 player 안에서 사용할 예정
    // => 따라서 GameRoom 안에서 접근을 하기 때문에 하나의 쓰레드에서만 접근을 하게 된다. (lock x)
    public class Inventory
    {
        Dictionary<int, Item> _items = new Dictionary<int, Item>();

        public void Add(Item item)
        {
            _items.Add(item.ItemDbId, item);
        }

        public Item Get(int itemDbId)
        {
            Item item = null;
            _items.TryGetValue(itemDbId, out item);

            return item;
        }
        
        public Item Find(Func<Item, bool> condition)
        {
            foreach (Item item in _items.Values)
            {
                if (condition.Invoke(item))
                    return item;
            }
            return null;
        }

        // 빈 슬롯이 있는지 체크
        // nullabe로 만들어서 빈슬록이 없는 경우도 챙겨주자
        public int? GetEmptySlot()
        {
            for (int slot = 0; slot < 20; slot++)
            {
                // Player가 들고 있는 인벤토리의 어느 슬롯이 비어 있는지를 확인
                Item item = _items.Values.FirstOrDefault(i => i.Slot == slot);
                if (item == null)
                    return slot;
            }
            return null;
        }
    }
}