using System;
using System.Collections.Concurrent;
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
	public class OutputWindowViewModel : ReactiveObject, ILoggerFactory
	{
		public ReactiveList<OutputWindowCategoryViewModel> Categories { get; } = new ReactiveList<OutputWindowCategoryViewModel>();

		// Existing loggers for the different categories. Can be accessed on multiple threads so must be concurrent.
		private ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();

		private Subject<OutputWindowCategoryViewModel> _newCategories = new Subject<OutputWindowCategoryViewModel>();

		public OutputWindowViewModel()
		{
			// Add category when adding a new logger.
			_newCategories.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(category =>
				{
					// TEMP HACK
					if (category.Name != "Log") return;

					Categories.Add(category);
				});
		}

		// ILoggerFactory interface
		public ILogger CreateLogger(string category)
		{
			return _loggers.GetOrAdd(category, key =>
			{
				var categoryVM = new OutputWindowCategoryViewModel(key);

				// Fire new categories observable.
				_newCategories.OnNext(categoryVM);

				return categoryVM.Logger;
			});
		}
	}
}
