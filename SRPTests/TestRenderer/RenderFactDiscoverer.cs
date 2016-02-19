using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SRPTests.TestRenderer
{
	public class RenderFactDiscoverer : FactDiscoverer
	{
		private readonly IMessageSink _diagnosticMessageSink;

		public RenderFactDiscoverer(IMessageSink diagnosticMessageSink)
			: base(diagnosticMessageSink)
		{
			_diagnosticMessageSink = diagnosticMessageSink;
		}

		protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
		{
			return new RenderTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);
		}
	}

	public class RenderTheoryDiscoverer : TheoryDiscoverer
	{
		private readonly IMessageSink _diagnosticMessageSink;

		public RenderTheoryDiscoverer(IMessageSink diagnosticMessageSink)
			: base(diagnosticMessageSink)
		{
			_diagnosticMessageSink = diagnosticMessageSink;
		}

		protected override IXunitTestCase CreateTestCaseForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow)
		{
			return new RenderTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, dataRow);
		}

		protected override IXunitTestCase CreateTestCaseForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
		{
			return new RenderTheoryTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);
		}
	}
}
