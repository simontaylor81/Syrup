using SRPCommon.Interfaces;
using System;
using System.Collections.Generic;
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
			throw new NotImplementedException();
		}
	}
}
