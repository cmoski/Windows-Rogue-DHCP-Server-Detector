using System;
using System.Collections.Generic;

namespace RogueChecker;

public class CommonUI
{
	private List<string> AuthorizedServers;

	private List<RogueData> RogueServerData;

	private static CommonUI objCommon;

	public bool StoreLoaded;

	public List<RogueData> RogueServers => RogueServerData;

	private CommonUI()
	{
	}

	public static CommonUI CreateCommonInstance()
	{
		if (objCommon == null)
		{
			objCommon = new CommonUI();
		}
		return objCommon;
	}

	public void AddAuthorizedServer(string szAuthServer)
	{
		if (AuthorizedServers == null)
		{
			AuthorizedServers = new List<string>();
		}
		AuthorizedServers.Add(szAuthServer);
	}

	public void CreateRogueEntry(string TransactionID)
	{
		RogueData rogueData = null;
		if (RogueServerData == null)
		{
			RogueServerData = new List<RogueData>();
		}
		rogueData = new RogueData();
		rogueData.TransactionID = TransactionID;
		rogueData.StartTime = DateTime.Now;
		RogueServerData.Add(rogueData);
	}

	public bool AddRogueServer(string szRogueServer, string szClientIP, string szGatewayIP, string szTransactionID, string MacID)
	{
		bool result = false;
		if (!ServerExists(szRogueServer))
		{
			RogueData rogue = GetRogue(szTransactionID);
			if (rogue != null)
			{
				rogue.ServerIP = szRogueServer;
				rogue.ClientIP = szClientIP;
				rogue.GatewayIP = szGatewayIP;
				rogue.EndTime = DateTime.Now;
				rogue.ResponseTime = rogue.EndTime - rogue.StartTime;
				result = true;
			}
		}
		return result;
	}

	private RogueData GetRogue(string szTransactionID)
	{
		foreach (RogueData rogueServerDatum in RogueServerData)
		{
			if (rogueServerDatum.TransactionID == szTransactionID)
			{
				if (string.IsNullOrEmpty(rogueServerDatum.ServerIP))
				{
					return rogueServerDatum;
				}
				RogueData rogueData = new RogueData();
				rogueData.TransactionID = rogueServerDatum.TransactionID;
				rogueData.StartTime = rogueServerDatum.StartTime;
				RogueServerData.Add(rogueData);
				return rogueData;
			}
		}
		return null;
	}

	private bool ServerExists(string ServerIP)
	{
		foreach (RogueData rogueServerDatum in RogueServerData)
		{
			if (rogueServerDatum.ServerIP == ServerIP)
			{
				return true;
			}
		}
		return false;
	}

	public bool CheckIfRogue(string ServerIP)
	{
		if (AuthorizedServers == null)
		{
			return false;
		}
		foreach (string authorizedServer in AuthorizedServers)
		{
			if (ServerIP.Equals(authorizedServer))
			{
				return true;
			}
		}
		return false;
	}
}
