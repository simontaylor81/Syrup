using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Scripting
{
	// Class representing a script, from a file, that can be executed.
	public class Script
	{
		private readonly string _filename;

		public Script(string filename)
		{
			_filename = filename;
		}

		public async Task<string> GetCodeAsync()
		{
			using (var reader = File.OpenText(_filename))
			{
				return await reader.ReadToEndAsync();
			}
		}
	}
}
