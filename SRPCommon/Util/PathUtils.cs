using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Util
{
	// Misc path utils.
	public static class PathUtils
	{
		// Compare two paths for equality.
		public static bool PathsEqual(string path1, string path2)
		{
			// Note: this doesn't support non-windows platforms currently.
			return Path.GetFullPath(path1).Equals(Path.GetFullPath(path2), StringComparison.OrdinalIgnoreCase);
		}
	}
}
