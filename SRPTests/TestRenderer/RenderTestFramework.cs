using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

// Use this test framework for the whole assembly.
// In a more realistic scenario, this would have to go on the test package
// rather than in the framework.
[assembly: TestFramework("SRPTests.TestRenderer.RenderTestFramework", "SRPTests")]

namespace SRPTests.TestRenderer
{
	public class RenderTestFramework : XunitTestFramework
	{
		public RenderTestFramework(IMessageSink messageSink)
			: base(messageSink)
		{ }

		protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
			=> new RenderTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
	}

	public class RenderTestFrameworkExecutor : XunitTestFrameworkExecutor
	{
		public RenderTestFrameworkExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
			: base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
		{ }

		protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
		{
			using (var assemblyRunner = new RenderTestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions))
				await assemblyRunner.RunAsync();
		}
	}

	public class RenderTestAssemblyRunner : XunitTestAssemblyRunner
	{
		public RenderTestAssemblyRunner(TestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
			: base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
		{ }

		protected override async Task AfterTestAssemblyStartingAsync()
		{
			await base.AfterTestAssemblyStartingAsync();
			await Aggregator.RunAsync(TestReporter.StaticInitAsync);
		}

		protected override async Task BeforeTestAssemblyFinishedAsync()
		{
			await Aggregator.RunAsync(TestReporter.StaticDisposeAsync);
			await base.BeforeTestAssemblyFinishedAsync();
		}

		protected override IMessageBus CreateMessageBus()
		{
			return new DelegatingMessageBus(base.CreateMessageBus(), msg =>
			{
				Console.WriteLine(msg.ToString());
			});
		}

		protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
		{
			return new RenderTestCollectionRunner(
				testCollection,
				testCases,
				DiagnosticMessageSink,
				messageBus,
				TestCaseOrderer,
				new ExceptionAggregator(Aggregator),
				cancellationTokenSource
			).RunAsync();
		}
	}

	public class RenderTestCollectionRunner : XunitTestCollectionRunner
	{
		private readonly IMessageSink _diagnosticMessageSink;

		public RenderTestCollectionRunner(
			ITestCollection testCollection,
			IEnumerable<IXunitTestCase> testCases,
			IMessageSink diagnosticMessageSink,
			IMessageBus messageBus,
			ITestCaseOrderer testCaseOrderer,
			ExceptionAggregator aggregator,
			CancellationTokenSource cancellationTokenSource)
			: base(testCollection,
				testCases,
				diagnosticMessageSink,
				messageBus,
				testCaseOrderer,
				aggregator,
				cancellationTokenSource)
		{
			_diagnosticMessageSink = diagnosticMessageSink;
		}

		protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
		{
			return new RenderTestClassRunner(testClass, @class, testCases, _diagnosticMessageSink, MessageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), CancellationTokenSource, CollectionFixtureMappings).RunAsync();
		}
	}

	public class RenderTestClassRunner : XunitTestClassRunner
	{
		private readonly RenderTestContext _context;

		public RenderTestClassRunner(ITestClass testClass,
									 IReflectionTypeInfo @class,
									 IEnumerable<IXunitTestCase> testCases,
									 IMessageSink messageSink,
									 IMessageBus messageBus,
									 ITestCaseOrderer testCaseOrderer,
									 ExceptionAggregator aggregator,
									 CancellationTokenSource cancellationTokenSource,
									 IDictionary<Type, object> collectionFixtureMappings)
			: base(testClass, @class, testCases, messageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource, collectionFixtureMappings)
		{
			Console.WriteLine("In RenderTestClassRunner()");
			_context = new RenderTestContext();
		}

		protected override bool TryGetConstructorArgument(ConstructorInfo constructor, int index, ParameterInfo parameter, out object argumentValue)
		{
			if (parameter.ParameterType == typeof(RenderTestContext))
			{
				argumentValue = _context;
				return true;
			}
			return base.TryGetConstructorArgument(constructor, index, parameter, out argumentValue);
		}

		//protected override async Task AfterTestClassStartingAsync()
		//{
		//	await base.AfterTestClassStartingAsync();

		//	Console.WriteLine("In AfterTestClassStartingAsync");
		//	ClassFixtureMappings[typeof(RenderTestContext)] = _context;
		//}

		protected override Task<RunSummary> RunTestMethodAsync(ITestMethod testMethod, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, object[] constructorArguments)
			=> new RenderTestMethodRunner(_context, testMethod, Class, method, testCases, DiagnosticMessageSink, MessageBus, new ExceptionAggregator(Aggregator), CancellationTokenSource, constructorArguments).RunAsync();
	}

	public class RenderTestMethodRunner : XunitTestMethodRunner
	{
		private readonly RenderTestContext _context;
		private readonly IMessageSink _diagnosticMessageSink;
		private readonly object[] _constructorArguments;

		public RenderTestMethodRunner(RenderTestContext context, ITestMethod testMethod, IReflectionTypeInfo @class, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ExceptionAggregator exceptionAggregator, CancellationTokenSource cancellationTokenSource, object[] constructorArguments)
			: base(testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, exceptionAggregator, cancellationTokenSource, constructorArguments)
		{
			_context = context;
			_diagnosticMessageSink = diagnosticMessageSink;
			_constructorArguments = constructorArguments;
		}

		protected override async Task<RunSummary> RunTestCaseAsync(IXunitTestCase testCase)
		{
			Console.WriteLine($"Running test case: {testCase.DisplayName}");
			var result = await testCase.RunAsync(_diagnosticMessageSink, MessageBus, _constructorArguments,
				new ExceptionAggregator(Aggregator), CancellationTokenSource);
			Console.WriteLine($"Test case complete: {testCase.DisplayName}");
			return result;
		}

	}
}
