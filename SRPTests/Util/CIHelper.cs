using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
		public static bool IsCI { get { return Provider != null; } }

		// Various properties of the build currently being built.
		public static string BuildNumber
		{
			get
			{
				if (Provider != null)
				{
					return Provider.BuildNumber;
				}
				return "";
			}
		}

		public static string Version
		{
			get
			{
				if (Provider != null)
				{
					return Provider.Version;
				}
				return "";
			}
		}

		public static string Commit
		{
			get
			{
				if (Provider != null)
				{
					return Provider.Commit;
				}
				return "";
			}
		}

		// Publish an artefact to the CI server.
		public static async Task PublishArtefact(string path)
		{
			if (Provider != null)
			{
				await Provider.PublishArtefactAsync(path).ConfigureAwait(false);
			}
		}

		private static ICIProvider CreateProvider()
		{
			return AppveyorCI.ConditionalCreate()
				?? DummyCIProvider.ConditionalCreate();
		}
	}

	// Interface for CI integration implementations.
	internal interface ICIProvider
	{
		string BuildNumber { get; }
		string Version { get; }
		string Commit { get; }

		// Publish an artefact to the CI server.
		Task PublishArtefactAsync(string path);
    }
}
