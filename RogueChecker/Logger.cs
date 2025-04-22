using System;

namespace RogueChecker;

public class Logger
{
	private static Logger logger;

	protected static string[] logLevelDesc = null;

	protected LoggerEventHandler[][] leh;

	protected uint max;

	protected uint levels;

	protected LoggerEventHandler defaultHandler;

	public Logger(uint levels, LoggerEventHandler defaultHandler)
	{
		init(levels, defaultHandler);
	}

	public Logger(uint levels, string filename)
	{
		init(levels, new BasicFileLogEventHandler(filename));
	}

	public Logger(uint levels)
	{
		init(levels, null);
	}

	public Logger promoteToStatic()
	{
		logger = this;
		return logger;
	}

	public static Logger singleton()
	{
		if (logger == null)
		{
			string filename = DateTime.Now.ToShortDateString().Replace("/", "-").Replace("\\", "-") + ".log";
			logger = new Logger(6u, filename);
			logLevelDesc = new string[6];
			logLevelDesc[0] = "V_CRITICAL";
			logLevelDesc[1] = "V_ERROR";
			logLevelDesc[2] = "V_WARN";
			logLevelDesc[3] = "V_INFO";
			logLevelDesc[4] = "V_DEBUG";
			logLevelDesc[5] = "V_ALL";
		}
		return logger;
	}

	private void init(uint levels, LoggerEventHandler defaultHandler)
	{
		this.levels = levels;
		this.defaultHandler = defaultHandler;
		max = levels - 1;
		leh = new LoggerEventHandler[levels][];
		LoggerEventHandler[] array = new LoggerEventHandler[1] { defaultHandler };
		for (int i = 0; i < levels; i++)
		{
			leh[i] = array;
		}
	}

	public void setMaximumLogLevel(uint max)
	{
		this.max = max;
	}

	public void SetLogFile(string filename)
	{
		defaultHandler.ChangeLogFile(filename);
	}

	public uint getMaximumLogLevel()
	{
		return max;
	}

	public LoggerEventHandler getDefaultLoggerEventHandler()
	{
		return defaultHandler;
	}

	public void addSpecialLoggerToAllLevels(LoggerEventHandler handler)
	{
		if (handler != null)
		{
			for (int i = 0; i < levels; i++)
			{
				addSpecialLogger(i, handler);
			}
		}
	}

	public void addSpecialLogger(int level, LoggerEventHandler handler)
	{
		if (level >= levels)
		{
			return;
		}
		if (leh[level] != null)
		{
			int num = leh[level].Length + 1;
			LoggerEventHandler[] array = new LoggerEventHandler[num];
			for (int i = 0; i < leh[level].Length; i++)
			{
				array[i] = leh[level][i];
			}
			array[num - 1] = handler;
			leh[level] = array;
		}
		else
		{
			leh[level] = new LoggerEventHandler[1];
			leh[level][0] = handler;
		}
	}

	public void addSpecialLogger(int level, string filename)
	{
		addSpecialLogger(level, new BasicFileLogEventHandler(filename));
	}

	public void log(LoggerLevel loglevel, string tag, string message)
	{
		if ((long)loglevel > (long)max || (long)loglevel >= (long)levels || leh[(int)loglevel] == null)
		{
			return;
		}
		for (int i = 0; i < leh[(int)loglevel].Length; i++)
		{
			if (logLevelDesc == null)
			{
				if (leh[(int)loglevel][i] != null)
				{
					leh[(int)loglevel][i].log(tag, (int)loglevel, "", message);
				}
			}
			else if (leh[(int)loglevel][i] != null)
			{
				leh[(int)loglevel][i].log(tag, (int)loglevel, logLevelDesc[(int)loglevel], message);
			}
		}
	}

	public void shutdown()
	{
		for (int i = 0; i < leh.Length; i++)
		{
			for (int j = 0; j < leh[i].Length; j++)
			{
				if (leh[i][j] != null)
				{
					leh[i][j].shutdown();
				}
			}
		}
	}
}
