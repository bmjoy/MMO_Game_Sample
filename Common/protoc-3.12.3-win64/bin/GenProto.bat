protoc.exe -I=./ --csharp_out=./ ./Protocol.proto 
IF ERRORLEVEL 1 PAUSE

START ../../../PacketGenerator/bin/Debug/net5.0/PacketGenerator.exe ./Protocol.proto
XCOPY /Y Protocol.cs "../../../Client/Assets/Scripts/Packet"
XCOPY /Y Protocol.cs "../../../Server/Server/Packet"
XCOPY /Y ClientPacketManager.cs "../../../Client/Assets/Scripts/Packet"
XCOPY /Y ServerPacketManager.cs "../../../Server/Server/Packet"