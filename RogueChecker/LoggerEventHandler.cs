using System;
using System.Collections;
using System.Threading;

namespace RogueChecker;

public abstract class LoggerEventHandler
{
	private bool alive;

	private Queue q;

	private Thread dispatch;

	protected string[] logLevelDescriptors;

	public LoggerEventHandler()
	{
		q = Queue.Synchronized(new Queue(1000));
		start();
	}

	public void start()
	{
		if (!alive)
		{
			alive = true;
			dispatch = new Thread(dispatchMessages);
			dispatch.Start();
		}
	}

	public void shutdown()
	{
		if (alive)
		{
			alive = false;
			Monitor.Enter(q);
			Monitor.PulseAll(q);
			Monitor.Exit(q);
		}
	}

	public void abort()
	{
		if (alive)
		{
			alive = false;
			dispatch.Abort();
		}
	}

	protected void dispatchMessages()
	{
		while (alive)
		{
			while (q.Count != 0 && alive)
			{
				log((LoggerMessage)q.Dequeue());
			}
			if (alive && q.Count == 0)
			{
				Monitor.Enter(q);
				if (q.Count == 0)
				{
					Monitor.Wait(q);
				}
				Monitor.Exit(q);
			}
		}
		while (q.Count != 0)
		{
			log((LoggerMessage)q.Dequeue());
		}
		onShutdown();
		dispatch = null;
	}

	public void log(string tag, int level, string level_desc, string message)
	{
		if (alive)
		{
			LoggerMessage loggerMessage = default(LoggerMessage);
			loggerMessage.message = message;
			loggerMessage.tag = tag;
			loggerMessage.level = level;
			loggerMessage.level_desc = level_desc;
			loggerMessage.time = DateTime.Now.ToFileTime();
			q.Enqueue(loggerMessage);
			Monitor.Enter(q);
			Monitor.PulseAll(q);
			Monitor.Exit(q);
		}
	}

	protected abstract void log(LoggerMessage message);

	protected abstract void onShutdown();

	public abstract void ChangeLogFile(string filename);
}
