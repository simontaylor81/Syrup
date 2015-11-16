using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Util
{
	// Observable that caches it's most recent value.
	// Basically a wrapper around BehaviorSubject.
	public class CacheLatestObservable<T> : IConnectableObservable<T>
	{
		private readonly IObservable<T> _source;
		private readonly BehaviorSubject<T> _subject;
		private bool _isConnected = false;

		public CacheLatestObservable(IObservable<T> source, T initialValue)
		{
			_subject = new BehaviorSubject<T>(initialValue);
			_source = source;
		}

		// Subscribe an observer to the subject.
		public IDisposable Subscribe(IObserver<T> observer)
		{
			return _subject.Subscribe(observer);
		}

		// Connect to the observable, which hooks up the subject to the source observable.
		public IDisposable Connect()
		{
			// Only allow single connection, for simplicity.
			if (_isConnected)
			{
				throw new InvalidOperationException("Cannot connect if already connected.");
			}

			// Subscribe subject to source.
			var disposable = _source.Subscribe(_subject);

			return Disposable.Create(() =>
			{
				_isConnected = false;
				disposable.Dispose();
			});
		}
	}

	// Helper method to create CacheLatestObservables easily.
	public static class CacheLatestObservableExtensions
	{
		// Cache the latest value of the observable using a BehaviorSubject so any subscriber
		// will always get at least that value.
		public static IConnectableObservable<T> CacheLatest<T>(this IObservable<T> source, T initialValue)
			=> new CacheLatestObservable<T>(source, initialValue);
	}
}
