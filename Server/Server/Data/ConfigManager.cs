using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Data
{
    [Serializable]
    public class ServerConfig
    {
        // Client에서 원본 파일을 관리한다는 가정
        public string dataPath;
    }
    
    // 일련의 설정 파일을 관리 => 데이터 경로, 서버 동접, 포트, 접속 IP 설정 등을 관리
    class ConfigManager
    {
        public static ServerConfig Config { get; private set; }
        public static void LoadConfig()
        {
            // config.json 파일의 경우는 실행 파일과 동일한 위치에 놓는 경우가 많다.
            // 
            string text = File.ReadAllText("config.json");
            Config = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerConfig>(text);

        }
    }
}
