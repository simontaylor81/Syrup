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
	public class OutputWindowCategoryViewModel : ReactiveObject
	{
		public string Name { get; }
		public ILogger Logger { get; }

		public IObservable<string> Messages { get; }
		public IObservable<Unit> Cleared { get; }

		private ObservableAsPropertyHelper<string> _text;
		public string Text => _text.Value;

		private StringBuilder _textBuilder = new StringBuilder();

		public OutputWindowCategoryViewModel(string name)
		{
			Name = name;

			var logger = new LoggerImpl();
			Logger = logger;

			// Subscribe to logger events to update the text output.
			var message = logger.Messages.ObserveOn(RxApp.MainThreadScheduler).Do(msg => _textBuilder.Append(msg));
			var cleared = logger.Cleared.ObserveOn(RxApp.MainThreadScheduler).Do(_ => _textBuilder.Clear());

			// Update text property when either occur.
			_text = message.Select(_ => Unit.Default).Merge(cleared)
				.Select(_ => _textBuilder.ToString())
				.ToProperty(this, x => x.Text, "");

			Messages = message;
			Cleared = cleared;
		}

		private class LoggerImpl : ILogger
		{
			private Subject<string> _messages = new Subject<string>();
			private Subject<Unit> _cleared = new Subject<Unit>();

			public IObservable<string> Messages => _messages;
			public IObservable<Unit> Cleared => _cleared;

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
}
