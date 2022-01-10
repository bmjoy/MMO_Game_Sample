using System;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;

namespace Server.DB
{  
    // 
    public class DbTransaction : JobSerializer
    {
        // 해당 로직을 담당하는 쓰레드를 딱 하나만 만들어서 관리 => static
        public static DbTransaction Instance { get; } = new DbTransaction();
        
        // 한방에 모두 실행하는 함수
        // Me(GameRoom): GameRoom이 실행하는 부분
        // -> You(Db): Db와 관련된 직원
        // -> Me (GameRoom): 일처리가 끝난 후 돌려줌
        public static void SavePlayerStatus_AllInOne(Player player, GameRoom room)
        {
            if (player == null || room == null)
                return;

            // Me(GameRoom)
            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.Stat.Hp;
            
            // You
            Instance.Push(() => 
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Entry(playerDb).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                    db.Entry(playerDb).Property(nameof(PlayerDb.Hp)).IsModified = true;

                    bool success = db.SaveChangesEx(); 
                    if (success)
                    {
                        // Me
                        room.Push(()=> System.Console.WriteLine($"Hp Saved ({playerDb.Hp})"));
                    }
                }
            });
        }
        
        public static void SavePlayerStatus_Step1(Player player, GameRoom room)
        {
            if (player == null || room == null)
                return;

            // Me(GameRoom)
            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.Stat.Hp;
            
            Instance.Push<PlayerDb, GameRoom>(SavePlayerStatus_Step2, playerDb, room);
        }

        public static void SavePlayerStatus_Step2(PlayerDb playerDb, GameRoom room)
        {
            using (AppDbContext db = new AppDbContext())
            {
                db.Entry(playerDb).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                db.Entry(playerDb).Property(nameof(PlayerDb.Hp)).IsModified = true;

                bool success = db.SaveChangesEx(); 
                if (success)
                {
                    // Me
                    room.Push(SavePlayerStatus_Step3, playerDb.Hp);
                }
            }
        }
        
        public static void SavePlayerStatus_Step3(int hp)
        {
            System.Console.WriteLine($"Hp Saved ({hp})");
        }

        // 플레이어에게 보상을 지급
        public static void RewardPlayer(Player player, RewardData rewardData, GameRoom room)
        {
            if (player == null || rewardData == null || room == null)
                return;

            // ToDo : 살짝 문제가 있긴 하다.
            int? slot = player.Inven.GetEmptySlot();
            if (slot == null)
                return;
            
            ItemDb itemDb = new ItemDb()
            {
                TemplateId = rewardData.itemId,
                Count = rewardData.count,
                Slot = slot.Value,
                OwnerDbId = player.PlayerDbId
            };

            // You
            Instance.Push(() => 
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Items.Add(itemDb);

                    bool success = db.SaveChangesEx(); 
                    if (success)
                    {
                        // Me
                        room.Push(() => 
                        {
                            Item newItem = Item.MakeItem(itemDb);
                            player.Inven.Add(newItem);

                            // Client Noti
                            {
                                S_AddItem itemPacket = new S_AddItem();
                                ItemInfo itemInfo = new ItemInfo();
                                itemInfo.MergeFrom(newItem.Info);
                                itemPacket.Items.Add(itemInfo);

                                player.Session.Send(itemPacket);
                            }
                        });
                    }
                }
            });
        }
    }
}