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
		public string FindProjectFile(string name)
		{
			// Look in shaders directory.
			return Path.Combine(GlobalConfig.BaseDir, @"SRPTests\TestScripts\Shaders", name);
		}
	}
}
