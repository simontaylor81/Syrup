using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPTests.TestRenderer
{
	public class RenderTestContext
	{
		public RenderTestHarness RenderTestHarness { get; }

		public RenderTestContext()
		{
			Trace.Assert(TestReporter.Instance != null);
			RenderTestHarness = new RenderTestHarness(TestReporter.Instance);
		}
	}
}
