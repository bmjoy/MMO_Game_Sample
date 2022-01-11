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
            player.HandleEquipItem(equipPacket);
        }
    }
}