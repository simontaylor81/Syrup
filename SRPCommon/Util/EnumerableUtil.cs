using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Util
{
	public class EnumerableUtil
	{
		public static IEnumerable<T> Range2D<T>(int numX, int numY, Func<int, int, T> selector)
		{
			for (int y = 0; y < numY; y++)
			{
				for (int x = 0; x < numX; x++)
				{
					yield return selector(x, y);
				}
			}
		}
	}
}
