using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Logging
{
	// Composite logger factory that forwards to multiple sub-logger factories.
	public class CompositeLoggerFactory : ILoggerFactory
	{
		// The sub-loggers that we forward messages to.
		private List<ILoggerFactory> _factories;

		public CompositeLoggerFactory()
		{
			_factories = new List<ILoggerFactory>();
		}

		public CompositeLoggerFactory(params ILoggerFactory[] factories)
		{
			_factories = factories.ToList();
		}

		public ILogger CreateLogger(string category)
		{
			// Create composite logger containing loggers from each factory.
			return new CompositeLogger(_factories.Select(f => f.CreateLogger(category)));
		}

		// Add a new factory to the list.
		public void AddFactory(ILoggerFactory factory)
		{
			_factories.Add(factory);
		}

		// Remove a factory from the list.
		public void RemoveFactory(ILoggerFactory factory)
		{
			_factories.Remove(factory);
		}

		// Clear all factories.
		public void RemoveAll()
		{
			_factories.Clear();
		}
	}
}
