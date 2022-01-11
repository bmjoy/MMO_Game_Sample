using System;
using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Game;

namespace Server.DB
{  
    // 
    public partial class DbTransaction : JobSerializer
    {
        public static void EquipItemNoti(Player player, Item item)
        {
            if (player == null || item == null)
                return;


            // ToDo : 살짝 문제가 있긴 하다.
            int? slot = player.Inven.GetEmptySlot();
            if (slot == null)
                return;
            

            ItemDb itemDb = new ItemDb()
            {
                ItemDbId = item.ItemDbId,
                Equipped = item.Equipped
            };

            // You
            Instance.Push(() => 
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Entry(itemDb).State = EntityState.Unchanged;
                    db.Entry(itemDb).Property(nameof(ItemDb.Equipped)).IsModified = true;

                    bool success = db.SaveChangesEx(); 
                    if (!success)
                    {
                        // 실패 했으면 Kick
                    }
                }
            });
        }

    }
}