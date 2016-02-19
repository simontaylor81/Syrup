using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SRPTests.TestRenderer
{
	[Serializable]
	public class RenderTestCase : XunitTestCase
	{
		public RenderTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay testMethodDisplay, ITestMethod testMethod, object[] testMethodArguments = null)
			: base(diagnosticMessageSink, testMethodDisplay, testMethod, testMethodArguments)
		{
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Called by the de-serializer", error: true)]
		public RenderTestCase() { }

		public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
		{
			//var context = new RenderTestContext(DisplayName);
			//constructorArguments = constructorArguments.StartWith(context).ToArray();

			return base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
		}
	}

	public class RenderTheoryTestCase : XunitTheoryTestCase
	{
		public RenderTheoryTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod)
			: base(diagnosticMessageSink, defaultMethodDisplay, testMethod)
		{ }

		/// <inheritdoc />
		public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
												  IMessageBus messageBus,
												  object[] constructorArguments,
												  ExceptionAggregator aggregator,
												  CancellationTokenSource cancellationTokenSource)
		{
			return new RenderTheoryTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource).RunAsync();
		}
	}

	public class RenderTheoryTestCaseRunner : XunitTheoryTestCaseRunner
	{
		public RenderTheoryTestCaseRunner(RenderTheoryTestCase renderTheoryTestCase, string displayName, string skipReason, object[] constructorArguments, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
			: base(renderTheoryTestCase, displayName, skipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource)
		{ }

		//protected override async Task AfterTestCaseStartingAsync()
		//{
		//	await base.AfterTestCaseStartingAsync();

		//	// Replace test runners with our own.
		//	testRun
		//}
	}
}
