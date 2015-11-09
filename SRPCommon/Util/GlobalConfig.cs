using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Util
{
	public static class GlobalConfig
	{
		// Application name to use for e.g. config file location.
		public static string AppName => "Syrup";

		// Base directory of the application (not the bin directory).
		public static string BaseDir
		{
			get
			{
				if (_baseDir == null)
				{
					// Start from the directory of the running executable, and move up until we're in the base dir.
					_baseDir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

					// If we have a "Shaders" directory, we've found the base dir.
					while (!Directory.Exists(Path.Combine(_baseDir, "Shaders")))
					{
						var newDir = Path.GetFullPath(Path.Combine(_baseDir, ".."));
						if (newDir == _baseDir)
						{
							// Moved up one but it didn't change anything, so we're in the drive root.
							throw new Exception("Could not find application base directory. Make sure the Shaders folder is present.");
						}
						_baseDir = newDir;
					}
				}
				return _baseDir;
			}
		}

		private static string _baseDir;
	}
}
