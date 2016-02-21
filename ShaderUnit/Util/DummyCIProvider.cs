using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderUnit.Util
{
	// Simple dummy CI provider for debugging.
	internal class DummyCIProvider : ICIProvider
	{
		// Create an instance if we're running in AppVeyor
		public static ICIProvider ConditionalCreate()
		{
			// Uncomment to use this when debugging stuff locally.
			//return new DummyCIProvider();
			return null;
		}

		// Pretend to be CI to exercise those code paths.
		public bool IsCI => true;
		public bool IsAppVeyor => false;
		public bool IsDummy => true;

		public string BuildNumber => "1";
		public string Version => "1.0.1";
		public string Commit => "abcdef";


		// "Publish" an artefact.
		public Task PublishArtefactAsync(string path)
		{
			Console.WriteLine("Publishing artefact {0}", path);

			// Just ShellExecute the file.
			Process.Start(path);

			// Return already-completed Task (this is fast-tracked internally)
			return Task.Delay(0);
		}

	}
}
