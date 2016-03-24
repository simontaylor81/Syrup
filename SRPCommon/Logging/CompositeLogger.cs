using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Logging
{
	// Composite logger that forwards messages to sub loggers.
	class CompositeLogger : ILogger
	{
		// The sub-loggers that we forward messages to.
		private readonly IEnumerable<ILogger> _loggers;

		public CompositeLogger(IEnumerable<ILogger> loggers)
		{
			// ToList to make concrete.
			_loggers = loggers.ToList();
		}

		public void Log(string message)
		{
			foreach (var logger in _loggers)
			{
				logger.Log(message);
			}
		}

		public void Clear()
		{
			foreach (var logger in _loggers)
			{
				logger.Clear();
			}
		}
	}
}
