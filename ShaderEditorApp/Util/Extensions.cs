using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.Util
{
	public static class Extensions
	{
		/// <summary>
		/// If the source enumerable is null, returns an empty enumerable of the same type.
		/// Otherwise, returns the source unmodified.
		/// </summary>
		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
		{
			return source ?? Enumerable.Empty<T>();
		}
	}
}
