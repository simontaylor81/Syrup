using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Logging
{
	// A stream that writes to a logger.
	public class LoggerStream : Stream
	{
		private ILogger _logger;

		public LoggerStream(ILogger logger)
		{
			_logger = logger;
		}

		// Stream interface.

		public override void Write(byte[] buffer, int offset, int count)
		{
			_logger.Log(Encoding.UTF8.GetString(buffer, offset, count));
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
