using System;
using System.Collections.Generic;
using SRPCommon.UserProperties;

namespace SRPCommon.Scripting
{
	// Class representing a script, from a file, that can be executed.
	public class Script
	{
		public string Filename { get; }

		public IDictionary<string, IUserProperty> UserProperties { get; } = new Dictionary<string, IUserProperty>();

		// Values to inject before execution. Used by auto testing.
		public IDictionary<string, object> TestParams { get; } = new Dictionary<string, object>();

		public Script(string filename)
		{
			Filename = filename;
		}
	}
}
