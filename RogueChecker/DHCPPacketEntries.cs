namespace RogueChecker;

public struct DHCPPacketEntries
{
	public byte Operation;

	public byte HardwareAddressType;

	public byte HardwareAddressLength;

	public byte HopCount;

	public byte[] TransactionID;

	public byte[] SecondsSinceBoot;

	public byte[] Reserved;

	public byte[] ClientIpAddress;

	public byte[] YourIpAddress;

	public byte[] BootstrapServerAddress;

	public byte[] RelayAgentIpAddress;

	public byte[] HardwareAddress;

	public byte[] HostName;

	public byte[] BootFileName;

	public byte[] Options;
}
