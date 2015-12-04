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

		// Global variables to set before execution. Only used by auto testing.
		public IDictionary<string, object> GlobalVariables { get; } = new Dictionary<string, object>();

		public Script(string filename)
		{
			Filename = filename;
		}
	}
}
