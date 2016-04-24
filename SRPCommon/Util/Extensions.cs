﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
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

		// Add an item to the list, and return the added item.
		public static TAdded AddAndReturn<TAdded, TElement>(this IList<TElement> list, TAdded newElement) where TAdded : TElement
		{
			list.Add(newElement);
			return newElement;
		}

		// List<T> has AddRange but IList<T> does not, which is annoying.
		public static void AddRange<T>(this IList<T> ilist, IEnumerable<T> collection)
		{
			// Use List<T> implementation if available (it should be more efficient).
			var list = ilist as List<T>;
			if (list != null)
			{
				list.AddRange(collection);
				return;
			}

			// Add each item one-by-one.
			foreach (var item in collection)
			{
				ilist.Add(item);
			}
		}

		// Add a disposable to the composite, and return the added item.
		public static T AddAndReturn<T>(this CompositeDisposable compositeDisposable, T newDisposable) where T : IDisposable
		{
			compositeDisposable.Add(newDisposable);
			return newDisposable;
		}

		// Filter on Optional values that are valid, and select the underlying value.
		public static IObservable<T> WhereHasValue<T>(this IObservable<T?> source) where T : struct
		{
			return source
				.Where(x => x.HasValue)
				.Select(x => x.Value);
		}

		// Filter on Optional values that are valid, and select the underlying value.
		public static IEnumerable<T> WhereHasValue<T>(this IEnumerable<T?> source) where T : struct
		{
			return source
				.Where(x => x.HasValue)
				.Select(x => x.Value);
		}
	}
}
