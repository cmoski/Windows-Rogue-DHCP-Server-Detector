using System.Net;
using System.Net.Sockets;

namespace RogueChecker;

public struct UdpState
{
	public IPEndPoint endPoint;

	public UdpClient client;
}
