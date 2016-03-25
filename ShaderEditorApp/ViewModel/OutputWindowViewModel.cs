using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ReactiveUI;
using SRPCommon.Logging;

namespace ShaderEditorApp.ViewModel
{
	// A filename and the position within it, for jumping to that file in the editor.
	public struct FileAndPosition
	{
		public string Filename;
		public int LineNumber;
		public int CharacterNumber;
	}

	public class OutputWindowViewModel : ReactiveObject, ILoggerFactory
	{
		public ReactiveList<OutputWindowCategoryViewModel> Categories { get; } = new ReactiveList<OutputWindowCategoryViewModel>();

		private OutputWindowCategoryViewModel _currentCategory;
		public OutputWindowCategoryViewModel CurrentCategory
		{
			get { return _currentCategory; }
			set { this.RaiseAndSetIfChanged(ref _currentCategory, value); }
		}

		public ReactiveCommand<object> ClearCurrent { get; }

		private Subject<FileAndPosition> _gotoFile = new Subject<FileAndPosition>();
		public IObservable<FileAndPosition> GotoFile => _gotoFile;

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

			ClearCurrent = ReactiveCommand.Create(this.WhenAnyValue(x => x.CurrentCategory).Select(current => current != null));
			ClearCurrent.Subscribe(_ => CurrentCategory.Clear());

			// Trigger GotoFile whenever a category does.
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

		// Attempt to go to the file & line on the given line (i.e. the user double clicked it).
		public void Goto(string line)
		{
			var destination = new FileAndPosition();

			var shaderErrorRegex = new Regex(@"([^\*\?""<>|]*)\(([0-9]+),([0-9]+)\):");
			var match = shaderErrorRegex.Match(line);
			if (match.Success)
			{
				destination.Filename = match.Groups[1].Value;
				destination.LineNumber = int.Parse(match.Groups[2].Value);
				destination.CharacterNumber = int.Parse(match.Groups[3].Value);
			}

			if (destination.Filename != null)
			{
				_gotoFile.OnNext(destination);
			}
		}
	}
}
