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

		private OutputWindowCategoryViewModel _currentCategory;
		public OutputWindowCategoryViewModel CurrentCategory
		{
			get { return _currentCategory; }
			set { this.RaiseAndSetIfChanged(ref _currentCategory, value); }
		}

		// Existing loggers for the different categories. Can be accessed on multiple threads so must be concurrent.
		private ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();

		private Subject<OutputWindowCategoryViewModel> _newCategories = new Subject<OutputWindowCategoryViewModel>();

		public OutputWindowViewModel()
		{
			// Add category when adding a new logger.
			_newCategories.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(category =>
				{
					Categories.Add(category);

					// Set current category if we don't have one.
					if (CurrentCategory == null)
					{
						CurrentCategory = category;
					}

					// We take clearing a category to mean "I'm about to write stuff here, you want to look at it"
					// So select it as the current category.
					category.Cleared.Subscribe(_ => CurrentCategory = category);
				});

			// Update visibilities when the current category changes.
			// We keep all the text boxes around so things like caret position aren't lost when switching.
			this.WhenAnyValue(x => x.CurrentCategory).Subscribe(current =>
			{
				foreach (var category in Categories)
				{
					category.IsVisible = category == current;
				}
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

				return categoryVM;
			});
		}
	}
}
