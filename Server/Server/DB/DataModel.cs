using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.DB
{
    [Table("Account")]
    public class AccountDb
    {
        public int AccountDbId { get; set; }
        public string AccountName { get; set; }
        public ICollection<PlayerDb> Players { get; set; }
    }

    [Table("Player")]
    public class PlayerDb
    {
        public int PlayerDbId { get; set; }
        public string PlayerName { get; set; }

        [ForeignKey("Account")]
        public int AccountDbId { get; set; }
        public AccountDb Account { get; set; } 

        public ICollection<ItemDb> Items { get; set; }

        public int Level { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Attack { get; set; }
        public float Speed { get; set; }
        public int TotalExp { get; set; }
    }

    // 하나의 아이템에 대한 정보라고 생각하면 될 듯
    [Table("Item")]
    public class ItemDb
    {
        // DB에서 Item을 구분하기 위한 ID
        public int ItemDbId { get; set; }
        // Data Sheet상 어떤 Item인지를 구분
        public int TemplateId { get; set; }
        public int Count { get; set; }
        // 슬롯 번호 
        // => 우리가 인벤토리에서 아이템을 배치하고 다시 껏다가 키면 원래 대로 있도록 해주기 위함
        // => 게임에 따라 다르지만 창고 또는 장착하고 있는 아이템 정보 또한 이렇게 슬롯으로 관리를 해주면 좋다.
        // => ex) 0~10: 내가 차고 있는 아이템, 11~50: 내가 소유하고 있는 아이템, 51~100: 창고 아이템
        public int Slot { get; set; }

        [ForeignKey("Owner")]
        public int? OwnerDbId { get; set; }
        public PlayerDb Owner { get; set; }
    }
}