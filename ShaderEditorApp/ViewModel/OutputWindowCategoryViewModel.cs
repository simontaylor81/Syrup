using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using SRPCommon.Logging;

namespace ShaderEditorApp.ViewModel
{
	public class OutputWindowCategoryViewModel : ReactiveObject, ILogger
	{
		public string Name { get; }

		private Subject<string> _messages = new Subject<string>();
		private Subject<Unit> _cleared = new Subject<Unit>();

		public IObservable<string> Messages { get; }
		public IObservable<Unit> Cleared { get; }

		private bool _isVisible = false;
		public bool IsVisible
		{
			get { return _isVisible; }
			set { this.RaiseAndSetIfChanged(ref _isVisible, value); }
		}

		public OutputWindowCategoryViewModel(string name)
		{
			Name = name;

			// Emit logger events on main thread for view consumption.
			Messages = _messages.ObserveOn(RxApp.MainThreadScheduler);
			Cleared = _cleared.ObserveOn(RxApp.MainThreadScheduler);
		}

		public void Log(string message)
		{
			_messages.OnNext(message);
		}

		public void Clear()
		{
			_cleared.OnNext(Unit.Default);
		}
	}
}
