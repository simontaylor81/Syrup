using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Util
{
	// Observable that caches it's most recent value.
	// Basically a wrapper around BehaviorSubject.
	public class CacheLatestObservable<T> : IObservable<T>, IDisposable
	{
		private BehaviorSubject<T> _subject;
		private IDisposable _disposable;

		public CacheLatestObservable(IObservable<T> source, T initialValue)
		{
			_subject = new BehaviorSubject<T>(initialValue);

			// Subscribe to source immediately.
			_disposable = source.Subscribe(_subject);
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			return _subject.Subscribe(observer);
		}

		public void Dispose()
		{
			_disposable.Dispose();
		}
	}

	// Helper method to create CacheLatestObservables easily.
	public static class CacheLatestObservableExtensions
	{
		// Cache the latest value of the observable using a BehaviorSubject so any subscriber
		// will always get at least that value.
		public static CacheLatestObservable<T> CacheLatest<T>(this IObservable<T> source, T initialValue)
		{
			return new CacheLatestObservable<T>(source, initialValue);
		}
	}
}
