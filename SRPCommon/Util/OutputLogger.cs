using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SRPCommon.Util
{
	public enum LogCategory
	{
		Log,			// General debug log message
		Script,			// Script output
		ShaderCompile,	// Shader compilation output
	}

	public interface ILogTarget
	{
		void Log(LogCategory category, string text);
	}

	public class OutputLogger
	{
		// Global instance. We don't enforce singleton in-case we want to create other instancing for some reason,
		// but most of the time you just want to use OutputLogger.Instance.
		private static Lazy<OutputLogger> instance = new Lazy<OutputLogger>(CreateDefaultInstance);
		public static OutputLogger Instance => instance.Value;

		public void AddTarget(ILogTarget target)
		{
			targets.Add(target);
		}

		public void Log(LogCategory category, string text)
		{
			foreach (var target in targets)
			{
				target.Log(category, text);
			}
		}

		public void LogLine(LogCategory category, string format)
		{
			Log(category, format + "\n");
		}
		public void LogLine(LogCategory category, string format, params object[] args)
		{
			Log(category, string.Format(format + "\n", args));
		}

		// Log a line to the output window, but only the first time it's called since the last call to ResetLogOnce.
		public void LogLineOnce(LogCategory category, string format, params object[] args)
		{
			// Format string to produce final line.
			var line = string.Format(format + "\n", args);

			// Use the resulting string as the key to prevent repetition.
			if (!logOnceLines.Contains(line))
			{
				Log(category, line);
				logOnceLines.Add(line);
			}
		}

		// Reset the set of logged-once lines, so everything will be logged again.
		public void ResetLogOnce()
		{
			logOnceLines.Clear();
		}


		public Stream GetStream(LogCategory category)
		{
			return new LogStream(this, category);
		}
		public StreamWriter GetStreamWriter(LogCategory category)
		{
			var stream = new LogStream(this, category);
			return new StreamWriter(stream, stream.StringEncoding);
		}

		private static OutputLogger CreateDefaultInstance()
		{
			var result = new OutputLogger();

			// Add a console target to the default instance.
			result.AddTarget(new ConsoleTarget());

			return result;
		}


		private List<ILogTarget> targets = new List<ILogTarget>();

		// Set of previously logged lines that should not be logged again.
		private HashSet<string> logOnceLines = new HashSet<string>();
	}

	class ConsoleTarget : ILogTarget
	{
		public void Log(LogCategory category, string text)
		{
			Console.Write(category.ToString() + ": " + text);
		}
	}

	// Stream that writes to the log.
	class LogStream : Stream
	{
		private OutputLogger	logger;
		private LogCategory		category;
		private Encoding		encoding = new UTF8Encoding();

		public LogStream(OutputLogger inLogger, LogCategory inCategory)
		{
			logger = inLogger;
			category = inCategory;
		}

		public Encoding StringEncoding => encoding;

		// Stream interface.

		public override void Write(byte[] buffer, int offset, int count)
		{
			logger.Log(category, encoding.GetString(buffer, offset, count));
		}

		// No Read support.
		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		// No Seeking.
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Flush()
		{
			// Nothing to flush.
		}

		public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanWrite => true;

		public override Int64 Length { get { throw new NotSupportedException(); } }
		public override long Position
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
	}
}
