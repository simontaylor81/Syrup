﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPTests.Util;

namespace SRPTests.TestRenderer
{
	// Interface for a test reporter implementation.
	interface ITestReporter : IDisposable
	{
		Task TestCompleteAsync(string name, bool bSucess, Bitmap result);
	}

	// Fixture class for reporting test results. Doesn't do the actual reporting,
	// just hands off to the actual reporter implementation that is appropriate.
	public class TestReporter : ITestReporter
	{
		private readonly ITestReporter _impl;

		public TestReporter()
		{
			if (FermiumReporter.CanUse)
			{
				// Use Fermium if we can.
				// I should finish Fermium one of these days...
				_impl = new FermiumReporter();
			}
			else if (!CIHelper.IsCI)
			{
				// Use simple file system writer when running locally.
				_impl = new FileSystemReporter();
			}
		}

		public void Dispose()
		{
			_impl?.Dispose();
		}

		public async Task TestCompleteAsync(string name, bool bSucess, Bitmap result)
		{
			if (_impl != null)
			{
				await _impl.TestCompleteAsync(name, bSucess, result);
			}
		}
	}
}
