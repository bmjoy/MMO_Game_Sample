using System.Linq;
using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.DB;
using ServerCore;

namespace Server
{
    public partial class ClientSession : PacketSession
    {
        // 게임에 들어가기 이전 상태에서 필요한 메서드들
        public void HandleLogin(C_Login loginPacket)
        {
            System.Console.WriteLine($"UniquedId({loginPacket.UniqueId}");

            // ToDo : 이런 저런 보안 체크
            if (ServerState != PlayerServerState.ServerStateLogin)
                return;

            // ToDo : 문제가 있긴 하다
            using (AppDbContext db = new AppDbContext())
            {
                AccountDb findAccount = db.Accounts
                    .Include(a => a.Players)
                    .Where(a => a.AccountName == loginPacket.UniqueId).FirstOrDefault();
                    
                if (findAccount != null)
                {
                    S_Login loginOk = new S_Login() { LoginOk = 1 };
                    Send(loginOk);
                }
                else
                {
                    AccountDb newAccount = new AccountDb() { AccountName = loginPacket.UniqueId };
                    db.Accounts.Add(newAccount);
                    db.SaveChanges();

                    S_Login loginOk = new S_Login() { LoginOk = 1 };
                    Send(loginOk);
                }
            }
        }
    }
}