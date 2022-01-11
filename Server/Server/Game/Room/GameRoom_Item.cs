using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using System;
using System.Collections.Generic;

namespace Server.Game
{
    public partial class GameRoom : JobSerializer
    {
        public void HandleEquipItem(Player player, C_EquipItem equipPacket)
        {
            // 나중에는 player가 해당 Room에 실제로 존재하는지 더블 체크 하는 것도 좋다
            if (player == null)
                return;
            Item item = player.Inven.Get(equipPacket.ItemDbId);
            if (item == null)
                return;
            
            // DB 연동
            // 메모리 선 적용
            item.Equipped = equipPacket.Equipped;

            // DB에 Noti
            // 이렇게 Noti만 날리고 잊고 살아도 된다.
            DbTransaction.EquipItemNoti(player, item); 

            // 클라에 통보
            S_EquipItem equipOkItem = new S_EquipItem();
            equipOkItem.ItemDbId = equipPacket.ItemDbId;
            equipOkItem.Equipped = equipPacket.Equipped;
            player.Session.Send(equipOkItem); 
        }
    }
}