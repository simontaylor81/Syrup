using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using ShaderUnit.Util;

// In reality, this would have to go in the test assembly.
[assembly: ShaderUnit.TestRenderer.UseTestReporter]

namespace ShaderUnit.TestRenderer
{
	// Interface for a test reporter implementation.
	interface ITestReporter
	{
		Task InitialiseAsync();
		Task TestCompleteAsync(string name, bool bSuccess, Bitmap result);
		Task DisposeAsync();
	}

	// Fixture class for reporting test results. Doesn't do the actual reporting,
	// just hands off to the actual reporter implementation that is appropriate.
	public class TestReporter : ITestReporter
	{
		private readonly ITestReporter _impl;

		public static TestReporter Instance { get; private set; }

		internal static Task StaticInitAsync()
		{
			Trace.Assert(Instance == null);
			Instance = new TestReporter();
			return Instance.InitialiseAsync();
		}

		internal static async Task StaticDisposeAsync()
		{
			if (Instance != null)
			{
				await Instance.DisposeAsync();
				Instance = null;
			}
		}

		public TestReporter()
		{
			if (FermiumReporter.CanUse)
			{
				// Use Fermium if we can.
				// I should finish Fermium one of these days...
				_impl = new FermiumReporter();
			}
			else if (CIHelper.IsCI)
			{
				// Write to dirty html file in CI if we don't have Fermium
				// (which we don't, cause I haven't written it yet).
				_impl = new HtmlReporter();
			}
			else
			{
				// Use simple file system writer when running locally.
				_impl = new FileSystemReporter();
			}
		}

		public async Task InitialiseAsync()
		{
			if (_impl != null)
			{
				await _impl.InitialiseAsync();
			}
		}

		public async Task DisposeAsync()
		{
			if (_impl != null)
			{
				await _impl.DisposeAsync();
			}
		}

		public async Task TestCompleteAsync(string name, bool bSuccess, Bitmap result)
		{
			if (_impl != null)
			{
				await _impl.TestCompleteAsync(name, bSuccess, result);
			}
		}
	}

	// Class for handling initialisation and disposal of the reporter.
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
	public class UseTestReporterAttribute : Attribute, ITestAction
	{
		public ActionTargets Targets => ActionTargets.Suite;

		public void BeforeTest(ITest test)
		{
			// TODO: Async?
			TestReporter.StaticInitAsync().Wait();
		}

		public void AfterTest(ITest test)
		{
			// TODO: Async?
			TestReporter.StaticDisposeAsync().Wait();
		}
	}
}
