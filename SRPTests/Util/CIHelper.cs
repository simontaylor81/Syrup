using System;
using System.Linq;
using System.Threading.Tasks;

namespace SRPTests.Util
{
	// Continuous integration helper functionality.
	// Currently supports Appveyor
	static class CIHelper
	{
		private static Lazy<ICIProvider> _provider = new Lazy<ICIProvider>(CreateProvider);
		private static ICIProvider Provider { get { return _provider.Value; } }

		// Are we running under a CI server?
		public static bool IsCI { get { return Provider.IsCI; } }

		// Various properties of the build currently being built.
		public static string BuildNumber { get { return Provider.BuildNumber; } }
		public static string Version { get { return Provider.Version; } }
		public static string Commit { get { return Provider.Commit; } }

		// Publish an artefact to the CI server.
		public static Task PublishArtefact(string path)
		{
			return Provider.PublishArtefactAsync(path);
		}

		private static ICIProvider CreateProvider()
		{
			return AppveyorCI.ConditionalCreate()
				?? DummyCIProvider.ConditionalCreate()
				?? new NullCIProvider();
		}

		// Very simple implementation of the CI Provider interface that returns null values.
		private class NullCIProvider : ICIProvider
		{
			public bool IsCI { get { return false; } }
			public string BuildNumber { get { return ""; } }
			public string Version { get { return ""; } }
			public string Commit { get { return ""; } }

			public Task PublishArtefactAsync(string path)
			{
				// Do nothing.
				return Task.Delay(0);
			}
		}
	}

	// Interface for CI integration implementations.
	internal interface ICIProvider
	{
		bool IsCI { get; }
		string BuildNumber { get; }
		string Version { get; }
		string Commit { get; }

		// Publish an artefact to the CI server.
		Task PublishArtefactAsync(string path);
    }
}
