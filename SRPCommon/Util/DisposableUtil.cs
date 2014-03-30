using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Util
{
	public static class DisposableUtil
	{
		// Dispose something if it isn't null.
		public static void SafeDispose(IDisposable obj)
		{
			if (null != obj)
				obj.Dispose();
		}

		// Dispose all items in a list, and clear the list.
		public static void DisposeList<T>(IList<T> resources) where T : IDisposable
		{
			foreach (var resource in resources)
				resource.Dispose();

			resources.Clear();
		}

		// Dispose all items in an array, then set the reference to null.
		public static void DisposeArray<T>(ref T[] resources) where T : IDisposable
		{
			if (resources != null)
			{
				foreach (var resource in resources)
					resource.Dispose();

				resources = null;
			}
		}
	}
}
