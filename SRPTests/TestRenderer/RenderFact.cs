using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace SRPTests.TestRenderer
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	[XunitTestCaseDiscoverer("SRPTests.TestRenderer.RenderFactDiscoverer", "SRPTests")]
	public class RenderFact : FactAttribute
	{
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	[XunitTestCaseDiscoverer("SRPTests.TestRenderer.RenderTheoryDiscoverer", "SRPTests")]
	public class RenderTheory : FactAttribute
	{
	}
}
