using System.IO;

namespace RogueChecker;

public class DhcpPacket
{
	public const int OPTION_OFFSET = 240;

	public DHCPPacketEntries dhcpPKTentries;

	public DhcpPacket()
	{
		dhcpPKTentries.TransactionID = new byte[4];
		dhcpPKTentries.SecondsSinceBoot = new byte[2];
		dhcpPKTentries.Reserved = new byte[2];
		dhcpPKTentries.ClientIpAddress = new byte[4];
		dhcpPKTentries.YourIpAddress = new byte[4];
		dhcpPKTentries.BootstrapServerAddress = new byte[4];
		dhcpPKTentries.RelayAgentIpAddress = new byte[4];
		dhcpPKTentries.HardwareAddress = new byte[16];
		dhcpPKTentries.HostName = new byte[64];
		dhcpPKTentries.BootFileName = new byte[128];
	}

	public DhcpPacket(byte[] MsgInfo)
	{
		MemoryStream memoryStream = new MemoryStream(MsgInfo, 0, MsgInfo.Length);
		try
		{
			BinaryReader binaryReader = new BinaryReader(memoryStream);
			dhcpPKTentries.Operation = binaryReader.ReadByte();
			dhcpPKTentries.HardwareAddressType = binaryReader.ReadByte();
			dhcpPKTentries.HardwareAddressLength = binaryReader.ReadByte();
			dhcpPKTentries.HopCount = binaryReader.ReadByte();
			dhcpPKTentries.TransactionID = binaryReader.ReadBytes(4);
			dhcpPKTentries.SecondsSinceBoot = binaryReader.ReadBytes(2);
			dhcpPKTentries.Reserved = binaryReader.ReadBytes(2);
			dhcpPKTentries.ClientIpAddress = binaryReader.ReadBytes(4);
			dhcpPKTentries.YourIpAddress = binaryReader.ReadBytes(4);
			dhcpPKTentries.BootstrapServerAddress = binaryReader.ReadBytes(4);
			dhcpPKTentries.RelayAgentIpAddress = binaryReader.ReadBytes(4);
			dhcpPKTentries.HardwareAddress = binaryReader.ReadBytes(16);
			dhcpPKTentries.HostName = binaryReader.ReadBytes(64);
			dhcpPKTentries.BootFileName = binaryReader.ReadBytes(128);
			dhcpPKTentries.Options = binaryReader.ReadBytes(MsgInfo.Length - 240);
		}
		catch
		{
		}
		finally
		{
			memoryStream?.Dispose();
			memoryStream = null;
			BinaryReader binaryReader = null;
		}
	}
}
