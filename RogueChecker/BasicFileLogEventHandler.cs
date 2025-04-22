using System;
using System.IO;
using System.Text;

namespace RogueChecker;

public class BasicFileLogEventHandler : LoggerEventHandler
{
	private StreamWriter stream;

	private bool append = true;

	public BasicFileLogEventHandler(string filename)
	{
		if (filename != null)
		{
			stream = new StreamWriter(new FileStream(filename, (!append) ? FileMode.Create : FileMode.Append, FileAccess.Write, FileShare.Read), Encoding.UTF8, 4096);
		}
	}

	public override void ChangeLogFile(string Dirpath)
	{
		if (Dirpath != null)
		{
			if (stream != null)
			{
				stream.Flush();
				stream.Close();
				append = false;
			}
			FileMode mode = ((!append) ? FileMode.Create : FileMode.Append);
			string path = Dirpath + "\\" + DateTime.Now.ToShortDateString().Replace("/", "-").Replace("\\", "-") + ".log";
			FileStream fileStream = new FileStream(path, mode, FileAccess.Write, FileShare.Read);
			stream = new StreamWriter(fileStream, Encoding.UTF8, 4096);
		}
	}

	protected override void log(LoggerMessage message)
	{
		if (stream != null)
		{
			string text = DateTime.FromFileTime(message.time).ToString();
			_ = "[" + text + " [" + message.level + ":" + message.level_desc + " (" + message.tag + ")] " + message.message + " ]\r\n";
			stream.Write("[" + text + " [" + message.level + ":" + message.level_desc + " (" + message.tag + ")] " + message.message + " ]\r\n");
		}
	}

	protected override void onShutdown()
	{
		if (stream != null)
		{
			stream.Flush();
			stream.Close();
		}
	}

	public void setAppend(bool flag)
	{
		append = flag;
	}

	public bool getAppend()
	{
		return append;
	}
}
