using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RogueChecker;

public class RecievePacketProcessing
{
	public delegate void DataRecievedEventHandler(byte[] Data, object StateInfo);

	public UdpState udpState;

	private object StateInfo;

	private Timer stateTimer;

	private CommonUI uiData;

	public event DataRecievedEventHandler DataRecieved;

	public RecievePacketProcessing()
	{
		uiData = CommonUI.CreateCommonInstance();
	}

	public RecievePacketProcessing(UdpState State, object st)
	{
		uiData = CommonUI.CreateCommonInstance();
		StateInfo = st;
		udpState = State;
		Thread.Sleep(100);
		StartListener();
	}

	~RecievePacketProcessing()
	{
		try
		{
			StopListener();
			if (udpState.client != null)
			{
				udpState.client.Close();
			}
			udpState.client = null;
			udpState.endPoint = null;
		}
		catch (Exception)
		{
		}
	}

	private void StartListener()
	{
		try
		{
			AutoResetEvent state = new AutoResetEvent(initialState: false);
			TimerCallback callback = scheduler;
			stateTimer = new Timer(callback, state, 5000, 0);
			IniListnerCallBack();
		}
		catch (Exception)
		{
			this.DataRecieved(null, StateInfo);
		}
	}

	private void IniListnerCallBack()
	{
		try
		{
			udpState.client.BeginReceive(OnDataRecieved, udpState);
		}
		catch (Exception)
		{
		}
	}

	public void StopListener()
	{
		try
		{
			if (udpState.client != null)
			{
				udpState.client.Close();
			}
			udpState.client = null;
			udpState.endPoint = null;
		}
		catch (Exception)
		{
		}
	}

	public void scheduler(object StateInfo)
	{
		StopListener();
		stateTimer.Dispose();
		this.DataRecieved(null, this.StateInfo);
	}

	public void OnDataRecieved(IAsyncResult asyn)
	{
		try
		{
			UdpState udpState = (UdpState)asyn.AsyncState;
			UdpClient client = udpState.client;
			IPEndPoint remoteEP = udpState.endPoint;
			byte[] parameter = client.EndReceive(asyn, ref remoteEP);
			Thread thread = new Thread(DataUpdation);
			thread.Start(parameter);
		}
		catch (Exception)
		{
		}
		finally
		{
			IPEndPoint remoteEP = null;
			UdpClient client = null;
			byte[] parameter = null;
			IniListnerCallBack();
		}
	}

	private void DataUpdation(object RecieveBytes)
	{
		byte[] data = (byte[])RecieveBytes;
		this.DataRecieved(data, StateInfo);
	}
}
