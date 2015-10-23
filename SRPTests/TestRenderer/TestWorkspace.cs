using SRPCommon.Interfaces;
using SRPCommon.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPTests.TestRenderer
{
	// Implementation of IWorkspace that finds test files.
	class TestWorkspace : IWorkspace
	{
		private readonly Dictionary<string, string> _files;

		public TestWorkspace()
		{
			// Find all files in the TestScripts dir.
			_files = Directory.EnumerateFiles(Path.Combine(GlobalConfig.BaseDir, @"SRPTests\TestScripts"), "*", SearchOption.AllDirectories)
				.ToDictionary(path => Path.GetFileName(path));
		}

		public string FindProjectFile(string name)
		{
			string path;
			if (_files.TryGetValue(name, out path))
			{
				return path;
			}
			return null;
		}

		public string GetAbsolutePath(string path)
		{
			return path;
		}
	}
}
