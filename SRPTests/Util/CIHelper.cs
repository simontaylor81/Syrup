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
		private static ICIProvider Provider => _provider.Value;

		// Are we running under a CI server?
		public static bool IsCI => Provider.IsCI;

		// Are we running under AppVeyor?
		public static bool IsAppVeyor => Provider.IsAppVeyor;

		// Are we running in the dummy CI environmnet?
		public static bool IsDummy => Provider.IsDummy;

		// Various properties of the build currently being built.
		public static string BuildNumber => Provider.BuildNumber;
		public static string Version => Provider.Version;
		public static string Commit => Provider.Commit;

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
			public bool IsCI => false;
			public bool IsAppVeyor => false;
			public bool IsDummy => false;
			public string BuildNumber => "";
			public string Version => "";
			public string Commit => "";

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
		bool IsAppVeyor { get; }
		bool IsDummy { get; }

		string BuildNumber { get; }
		string Version { get; }
		string Commit { get; }

		// Publish an artefact to the CI server.
		Task PublishArtefactAsync(string path);
	}
}
