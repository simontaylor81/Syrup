using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Util
{
	public static class Extensions
	{
		/// <summary>
		/// If the source enumerable is null, returns an empty enumerable of the same type.
		/// Otherwise, returns the source unmodified.
		/// </summary>
		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
			=> source ?? Enumerable.Empty<T>();

		// Remove all items from a list that satisfy a predicate.
		// Note: this already exists for List<T> (as RemoveAll) but not for IList<T>, infuriatingly.
		public static void RemoveByPredicate<T>(this IList<T> list, Predicate<T> predicate)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (predicate(list[i]))
				{
					list.RemoveAt(i--);
				}
			}
		}

		// Start the observable with its default value.
		public static IObservable<T> StartWithDefault<T>(this IObservable<T> source)
			=> source.StartWith(default(T));
	}
}
