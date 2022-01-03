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
    }
}
