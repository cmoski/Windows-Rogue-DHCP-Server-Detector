using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RogueChecker;

public class SendPacketProcessing
{
	public delegate void DataSendEventHandler();

	private const byte BOOT_REQUEST = 1;

	private const byte BOOT_REPLY = 2;

	private const byte PortToListenOn = 68;

	private const byte PortToSendTo = 67;

	private DhcpPacket dhcpPacketInfo;

	public RecievePacketProcessing RcvPacket;

	private static object locker = new object();

	private List<string> BindingNIC;

	public List<string> NICtoBind
	{
		get
		{
			return BindingNIC;
		}
		set
		{
			BindingNIC = value;
		}
	}

	public event DataSendEventHandler SendData;

	private List<string> RetrieveNICs(IPVersion IPv)
	{
		List<string> list = new List<string>();
		NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
		NetworkInterface[] array = allNetworkInterfaces;
		foreach (NetworkInterface networkInterface in array)
		{
			IPInterfaceProperties iPProperties = networkInterface.GetIPProperties();
			UnicastIPAddressInformationCollection unicastAddresses = iPProperties.UnicastAddresses;
			if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback || networkInterface.OperationalStatus == OperationalStatus.Down)
			{
				continue;
			}
			foreach (UnicastIPAddressInformation item in unicastAddresses)
			{
				if (item.Address.AddressFamily == AddressFamily.InterNetworkV6 && IPv == IPVersion.IPv6)
				{
					list.Add(item.Address.ToString());
				}
				else if (item.Address.AddressFamily == AddressFamily.InterNetwork && IPv == IPVersion.IPv4)
				{
					list.Add(item.Address.ToString());
				}
			}
		}
		return list;
	}

	public SendPacketProcessing()
	{
		CommonUI.CreateCommonInstance();
	}

	~SendPacketProcessing()
	{
	}

	public void PrepareDHCP(byte[] MacID)
	{
		dhcpPacketInfo = new DhcpPacket();
		dhcpPacketInfo.dhcpPKTentries.Operation = 1;
		dhcpPacketInfo.dhcpPKTentries.HardwareAddressType = 1;
		dhcpPacketInfo.dhcpPKTentries.HardwareAddressLength = 6;
		dhcpPacketInfo.dhcpPKTentries.HardwareAddress[0] = MacID[0];
		dhcpPacketInfo.dhcpPKTentries.HardwareAddress[1] = MacID[1];
		dhcpPacketInfo.dhcpPKTentries.HardwareAddress[2] = MacID[2];
		dhcpPacketInfo.dhcpPKTentries.HardwareAddress[3] = MacID[3];
		dhcpPacketInfo.dhcpPKTentries.HardwareAddress[4] = MacID[4];
		dhcpPacketInfo.dhcpPKTentries.HardwareAddress[5] = MacID[5];
		dhcpPacketInfo.dhcpPKTentries.HopCount = 0;
		dhcpPacketInfo.dhcpPKTentries.SecondsSinceBoot = new byte[2] { 0, 20 };
		dhcpPacketInfo.dhcpPKTentries.Reserved = new byte[2] { 128, 0 };
		dhcpPacketInfo.dhcpPKTentries.ClientIpAddress = IPAddress.Parse("0.0.0.0").GetAddressBytes();
		dhcpPacketInfo.dhcpPKTentries.YourIpAddress = IPAddress.Parse("0.0.0.0").GetAddressBytes();
		dhcpPacketInfo.dhcpPKTentries.BootstrapServerAddress = IPAddress.Parse("0.0.0.0").GetAddressBytes();
		dhcpPacketInfo.dhcpPKTentries.RelayAgentIpAddress = IPAddress.Parse("0.0.0.0").GetAddressBytes();
		CreateOptionStruct();
	}

	public string ByteToString(byte[] dByte, byte hLength)
	{
		try
		{
			string text = string.Empty;
			if (dByte != null)
			{
				for (int i = 0; i < hLength; i++)
				{
					text += dByte[i].ToString("X2");
				}
			}
			return text;
		}
		catch (Exception)
		{
			return string.Empty;
		}
		finally
		{
			string text = null;
		}
	}

	public DHCPMessageType GetMsgType(ref DhcpPacket lDhcp)
	{
		try
		{
			byte[] optionData = GetOptionData(DHCPOptions.DHCPMessageTYPE, ref lDhcp);
			if (optionData != null)
			{
				return (DHCPMessageType)optionData[0];
			}
		}
		catch (Exception)
		{
		}
		return (DHCPMessageType)0;
	}

	private void CreateOptionStruct()
	{
		try
		{
			dhcpPacketInfo.dhcpPKTentries.Options = null;
			AddMagicCookie(new byte[4] { 99, 130, 83, 99 }, ref dhcpPacketInfo.dhcpPKTentries.Options);
			CreateOptionElement(DHCPOptions.ClientIdentifier, new byte[6] { 0, 31, 59, 91, 189, 173 }, ref dhcpPacketInfo.dhcpPKTentries.Options);
			CreateOptionElement(DHCPOptions.HostName, Encoding.ASCII.GetBytes("Rogue"), ref dhcpPacketInfo.dhcpPKTentries.Options);
			CreateOptionElement(DHCPOptions.DHCPMessageTYPE, new byte[1] { 1 }, ref dhcpPacketInfo.dhcpPKTentries.Options);
			CreateOptionElement(DHCPOptions.ParameterRequestList, new byte[1] { 3 }, ref dhcpPacketInfo.dhcpPKTentries.Options);
			Array.Resize(ref dhcpPacketInfo.dhcpPKTentries.Options, dhcpPacketInfo.dhcpPKTentries.Options.Length + 1);
			Array.Copy(new byte[1] { 255 }, 0, dhcpPacketInfo.dhcpPKTentries.Options, dhcpPacketInfo.dhcpPKTentries.Options.Length - 1, 1);
		}
		catch (Exception)
		{
		}
	}

	private void AddMagicCookie(byte[] DataToAdd, ref byte[] AddtoMe)
	{
		try
		{
			if (AddtoMe == null)
			{
				Array.Resize(ref AddtoMe, DataToAdd.Length);
			}
			Array.Copy(DataToAdd, 0, AddtoMe, 0, DataToAdd.Length);
		}
		catch (Exception)
		{
		}
	}

	private void CreateOptionElement(DHCPOptions Code, byte[] DataToAdd, ref byte[] AddtoMe)
	{
		try
		{
			byte[] array = new byte[DataToAdd.Length + 2];
			array[0] = (byte)Code;
			array[1] = (byte)DataToAdd.Length;
			Array.Copy(DataToAdd, 0, array, 2, DataToAdd.Length);
			if (AddtoMe == null)
			{
				Array.Resize(ref AddtoMe, array.Length);
			}
			else
			{
				Array.Resize(ref AddtoMe, AddtoMe.Length + array.Length);
			}
			Array.Copy(array, 0, AddtoMe, AddtoMe.Length - array.Length, array.Length);
		}
		catch (Exception)
		{
		}
	}

	private byte[] GetOptionData(DHCPOptions DHCPTyp, ref DhcpPacket ldhcpInfo)
	{
		int num = 0;
		byte b = 0;
		try
		{
			num = (int)DHCPTyp;
			int num2;
			for (num2 = 4; num2 < ldhcpInfo.dhcpPKTentries.Options.Length; num2++)
			{
				byte b2 = ldhcpInfo.dhcpPKTentries.Options[num2];
				if (b2 == num)
				{
					b = ldhcpInfo.dhcpPKTentries.Options[num2 + 1];
					byte[] array = new byte[b];
					Array.Copy(ldhcpInfo.dhcpPKTentries.Options, num2 + 2, array, 0, b);
					return array;
				}
				b = ldhcpInfo.dhcpPKTentries.Options[num2 + 1];
				num2 += 1 + b;
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			byte[] array = null;
		}
		return null;
	}

	private byte[] BuildByteArrayofDHCPDiscoverPacket()
	{
		try
		{
			byte[] TargetArray = new byte[0];
			AddOptionElement(new byte[1] { dhcpPacketInfo.dhcpPKTentries.Operation }, ref TargetArray);
			AddOptionElement(new byte[1] { dhcpPacketInfo.dhcpPKTentries.HardwareAddressType }, ref TargetArray);
			AddOptionElement(new byte[1] { dhcpPacketInfo.dhcpPKTentries.HardwareAddressLength }, ref TargetArray);
			AddOptionElement(new byte[1] { dhcpPacketInfo.dhcpPKTentries.HopCount }, ref TargetArray);
			AddOptionElement(dhcpPacketInfo.dhcpPKTentries.TransactionID, ref TargetArray);
			AddOptionElement(dhcpPacketInfo.dhcpPKTentries.SecondsSinceBoot, ref TargetArray);
			AddOptionElement(dhcpPacketInfo.dhcpPKTentries.Reserved, ref TargetArray);
			AddOptionElement(dhcpPacketInfo.dhcpPKTentries.ClientIpAddress, ref TargetArray);
			AddOptionElement(dhcpPacketInfo.dhcpPKTentries.YourIpAddress, ref TargetArray);
			AddOptionElement(dhcpPacketInfo.dhcpPKTentries.BootstrapServerAddress, ref TargetArray);
			AddOptionElement(dhcpPacketInfo.dhcpPKTentries.RelayAgentIpAddress, ref TargetArray);
			AddOptionElement(dhcpPacketInfo.dhcpPKTentries.HardwareAddress, ref TargetArray);
			AddOptionElement(dhcpPacketInfo.dhcpPKTentries.HostName, ref TargetArray);
			AddOptionElement(dhcpPacketInfo.dhcpPKTentries.BootFileName, ref TargetArray);
			AddOptionElement(dhcpPacketInfo.dhcpPKTentries.Options, ref TargetArray);
			return TargetArray;
		}
		catch (Exception)
		{
			return null;
		}
		finally
		{
			byte[] TargetArray = null;
		}
	}

	private void AddOptionElement(byte[] FromValue, ref byte[] TargetArray)
	{
		try
		{
			if (TargetArray != null)
			{
				Array.Resize(ref TargetArray, TargetArray.Length + FromValue.Length);
			}
			else
			{
				Array.Resize(ref TargetArray, FromValue.Length);
			}
			Array.Copy(FromValue, 0, TargetArray, TargetArray.Length - FromValue.Length, FromValue.Length);
		}
		catch (Exception)
		{
		}
	}

	public void SendDHCPPacket(object StateInfo)
	{
		try
		{
			PrepareDHCP(new byte[6] { 0, 31, 59, 91, 189, 173 });
			if (BindingNIC.Count > 0)
			{
				for (int i = 0; i < BindingNIC.Count; i++)
				{
					Random random = new Random();
					random.NextBytes(dhcpPacketInfo.dhcpPKTentries.TransactionID);
					byte[] data = BuildByteArrayofDHCPDiscoverPacket();
					CreateSocketandSendMessage(BindingNIC[i], data, StateInfo);
				}
			}
			else
			{
				MessageBox.Show("Atleast one Interface should be active to send and recieve DHCP packets", "Rogue Detection", MessageBoxButtons.OK);
				((AutoResetEvent)StateInfo).Set();
			}
		}
		catch (Exception)
		{
		}
	}

	private void CreateSocketandSendMessage(string Nic, byte[] Data, object StateInfo)
	{
		IPEndPoint iPEndPoint = null;
		try
		{
			IPAddress address = IPAddress.Parse(Nic);
			iPEndPoint = new IPEndPoint(address, 68);
			UdpState udpState = default(UdpState);
			udpState.endPoint = iPEndPoint;
			udpState.client = new UdpClient(iPEndPoint);
			RcvPacket = new RecievePacketProcessing(udpState, StateInfo);
			RcvPacket.DataRecieved += dhcpDataFlow_DataRecieved;
			CommonUI commonUI = CommonUI.CreateCommonInstance();
			commonUI.CreateRogueEntry(ByteToString(dhcpPacketInfo.dhcpPKTentries.TransactionID, 4));
			udpState.client.BeginSend(Data, Data.Length, IPAddress.Broadcast.ToString(), 67, OnDataSent, udpState);
		}
		catch (Exception)
		{
			if (iPEndPoint != null)
			{
				MessageBox.Show("Interface: " + iPEndPoint.ToString() + " is used by DHCP Client for DHCP operation and cannot be used by Rogue detection tool\nConfigure the static IPv4 address for this interface, stop DHCP client and restart the application", "RogueChecker", MessageBoxButtons.OK);
			}
			dhcpDataFlow_DataRecieved(null, StateInfo);
		}
	}

	public void OnDataSent(IAsyncResult asyn)
	{
		try
		{
			((UdpState)asyn.AsyncState).client.EndSend(asyn);
		}
		catch (Exception)
		{
		}
	}

	public void dhcpDataFlow_DataRecieved(byte[] pktData, object StateInfo)
	{
		lock (locker)
		{
			if (pktData == null)
			{
				((AutoResetEvent)StateInfo).Set();
			}
			try
			{
				DhcpPacket lDhcp = new DhcpPacket(pktData);
				DHCPMessageType msgType = GetMsgType(ref lDhcp);
				string macID = ByteToString(lDhcp.dhcpPKTentries.HardwareAddress, lDhcp.dhcpPKTentries.HardwareAddressLength);
				DHCPMessageType dHCPMessageType = msgType;
				if (dHCPMessageType == DHCPMessageType.DHCPOFFER)
				{
					CommonUI commonUI = CommonUI.CreateCommonInstance();
					IPAddress iPAddress = IPAddress.Parse("0.0.0.0");
					byte[] optionData = GetOptionData(DHCPOptions.ServerIdentifier, ref lDhcp);
					byte[] optionData2 = GetOptionData(DHCPOptions.Router, ref lDhcp);
					IPAddress iPAddress2 = new IPAddress(optionData);
					IPAddress iPAddress3 = new IPAddress(lDhcp.dhcpPKTentries.YourIpAddress);
					if (optionData2 != null)
					{
						iPAddress = new IPAddress(optionData2);
					}
					if (!commonUI.CheckIfRogue(iPAddress2.ToString()) && commonUI.AddRogueServer(iPAddress2.ToString(), iPAddress3.ToString(), iPAddress.ToString(), ByteToString(lDhcp.dhcpPKTentries.TransactionID, 4), macID))
					{
						this.SendData();
					}
				}
			}
			catch (Exception)
			{
			}
		}
	}
}
