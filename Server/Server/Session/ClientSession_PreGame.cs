using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DB;
using Server.Game;
using ServerCore;

namespace Server
{
    public partial class ClientSession : PacketSession
    {
        public int AccountDbId { get; private set; }
        public List<LobbyPlayerInfo> LobbyPlayers { get; set; } = new List<LobbyPlayerInfo>();

        // 게임에 들어가기 이전 상태에서 필요한 메서드들
        public void HandleLogin(C_Login loginPacket)
        {
            System.Console.WriteLine($"UniquedId({loginPacket.UniqueId}");

            // ToDo : 이런 저런 보안 체크
            if (ServerState != PlayerServerState.ServerStateLogin)
                return;

            LobbyPlayers.Clear();

            // ToDo : 문제가 있긴 하다
            using (AppDbContext db = new AppDbContext())
            {
                AccountDb findAccount = db.Accounts
                    .Include(a => a.Players)
                    .Where(a => a.AccountName == loginPacket.UniqueId).FirstOrDefault();
                
                // 계정이 이미 있고 해당 계정에서 이미 생성했던 player들의 정보를 발송
                if (findAccount != null)
                {
                    // AccountDbId 메모리에 기억
                    AccountDbId = findAccount.AccountDbId;
                    S_Login loginOk = new S_Login() { LoginOk = 1 };
                    foreach (PlayerDb playerDb in findAccount.Players)
                    {
                        LobbyPlayerInfo lobbyPlayer = new LobbyPlayerInfo()
                        {
                            PlayerDbId = playerDb.PlayerDbId,
                            Name = playerDb.PlayerName,
                            StatInfo = new StatInfo()
                            {
                                Level = playerDb.Level,
                                Hp = playerDb.Hp,
                                MaxHp = playerDb.MaxHp,
                                Attack = playerDb.Attack,
                                Speed = playerDb.Speed,
                                TotalExp = playerDb.TotalExp,

                            }
                        };
                        // 메모리에도 들고 있는다
                        // => Player들의 정보를 얻어오기 위해 또 DB에 접근하는 것보다
                        // => 이렇게 메모리에 들고 있는 것이 좋다.
                        // 나중에 예를 들어 C_EnterGame 패킷을 통해 클라거 게임에 들어오고 싶다는 패킷을 보내게 될텐데
                        // 그때도 그 해당하는 플레이어를 내가 들고 있는 것이 맞는지 체크를 해야할 필요가 있다.
                        // 메모리에 들고 있는 것이 성능 상 좋다.
                        LobbyPlayers.Add(lobbyPlayer);

                        // 패킷에 넣어준다.
                        loginOk.Players.Add(lobbyPlayer);
                    }

                    Send(loginOk);
                    // 로비로 이동
                    ServerState = PlayerServerState.ServerStateLobby;
                }
                else
                {
                    AccountDb newAccount = new AccountDb() { AccountName = loginPacket.UniqueId };
                    db.Accounts.Add(newAccount);
                    bool success = db.SaveChangesEx(); 

                    if (success == false)
                        return;

                    // AccountDbId 메모리에 기억
                    AccountDbId = findAccount.AccountDbId;

                    S_Login loginOk = new S_Login() { LoginOk = 1 };
                    Send(loginOk);
                    ServerState = PlayerServerState.ServerStateLobby;
                }
            }
        }

        // 클라쪽에서 player를 선택 한 다음 Game에 들어올 때 작업
        public void HandleEnterGame(C_EnterGame enterGamePacket)
        {
            if (ServerState != PlayerServerState.ServerStateLobby)
                return;
            
            LobbyPlayerInfo playerInfo = LobbyPlayers.Find(p => p.Name == enterGamePacket.Name);
            if (playerInfo == null)
                return;
            
			MyPlayer = ObjectManager.Instance.Add<Player>();
			{
                MyPlayer.PlayerDbId = playerInfo.PlayerDbId;
				MyPlayer.Info.Name = playerInfo.Name;
				MyPlayer.Info.PosInfo.State = CreatureState.Idle;
				MyPlayer.Info.PosInfo.MoveDir = MoveDir.Down;
				MyPlayer.Info.PosInfo.PosX = 0;
				MyPlayer.Info.PosInfo.PosY = 0;
				MyPlayer.Stat.MergeFrom(playerInfo.StatInfo);
				MyPlayer.Session = this;
			}

            ServerState = PlayerServerState.ServerStateGame;

			GameRoom room = RoomManager.Instance.Find(1);
			room.Push(room.EnterGame, MyPlayer);
        }

        // Player를 생성하는 패킷 처리
        public void HandleCreatePlayer(C_CreatePlayer createaPacket)
        {
            // ToDo : 이런 저런 보안 체크
            if (ServerState != PlayerServerState.ServerStateLobby)
                return;
            using (AppDbContext db = new AppDbContext())
            {
                PlayerDb findPlayer = db.Players
                    .Where(p => p.PlayerName == createaPacket.Name).FirstOrDefault();
                if (findPlayer != null)
                {
                    // 이름이 겹친다
                    Send(new S_CreatePlayer());
                }
                // 새로운 player 생성
                else
                {
                    // 1렙 스탯 정보 추출
                    StatInfo stat = null;
                    DataManager.StatDict.TryGetValue(1, out stat);

                    // DB에 플레이어를 만들어줘야 함
                    PlayerDb newPlayerDb = new PlayerDb()
                    {
                        PlayerName = createaPacket.Name,
                        Level = stat.Level,
                        Hp = stat.Hp,
                        MaxHp = stat.MaxHp,
                        Attack = stat.Attack,
                        Speed = stat.Speed,
                        TotalExp = 0,
                        AccountDbId = this.AccountDbId
                    };

                    db.Players.Add(newPlayerDb);
                    // ToDo Exception Handling
                    // 동일한 이름을 다른 player가 생성 했을 수도 있기 때문에
                    // Exception이 날 수가 있다.
                    // 따라서 여기서 에러가 날 경우 클라에게 동일한 아이디가 생성 되었다는 패킷을 보내줘야 한다.

                    bool success = db.SaveChangesEx(); 

                    if (success == false)
                        return;

                    // 메모리에 추가
                    LobbyPlayerInfo lobbyPlayer = new LobbyPlayerInfo()
                    {
                        PlayerDbId = newPlayerDb.PlayerDbId,
                        Name = createaPacket.Name,
                        StatInfo = new StatInfo()
                        {
                            Level = stat.Level,
                            Hp = stat.Hp,
                            MaxHp = stat.MaxHp,
                            Attack = stat.Attack,
                            Speed = stat.Speed,
                            TotalExp = 0
                        }
                    };

                    // 메모리에도 들고 있다.
                    LobbyPlayers.Add(lobbyPlayer);

                    // 클라에 전송
                    S_CreatePlayer newPlayer = new S_CreatePlayer() { Player = new LobbyPlayerInfo() };
                    newPlayer.Player.MergeFrom(lobbyPlayer);

                    Send(newPlayer);
                }
            }
        }
    }
}