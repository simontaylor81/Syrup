using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using ReactiveUI;
using ShaderEditorApp.Interfaces;
using Splat;

namespace ShaderEditorApp.ViewModel
{
	// ViewModel class representing an open document.
	public class DocumentViewModel : ReactiveObject, IDisposable
	{
		// Create a new (empty) document.
		public DocumentViewModel(OpenDocumentSetViewModel openDocumentSet, IUserPrompt userPrompt = null, IIsForegroundService isForeground = null)
		{
			_openDocumentSet = openDocumentSet;
			Document = new TextDocument();

			// Dirty when document changes.
			Document.TextChanged += (o, e) => IsDirty = true;

			_userPrompt = userPrompt ?? Locator.Current.GetService<IUserPrompt>();
			_isForeground = isForeground ?? Locator.Current.GetService<IIsForegroundService>();

			// Update display name based on filename and dirtiness.
			this.WhenAnyValue(x => x.FilePath, x => x.IsDirty, (filename, isDirty) => GetDisplayName(filename, isDirty))
				.ToProperty(this, x => x.DisplayName, out _displayName);

			// Set syntax highlighting definition to use based on extension.
			_syntaxHighlighting = this.WhenAnyValue(x => x.FilePath)
				.Select(path => path != null
					? HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path))
					: null)
				.ToProperty(this, x => x.SyntaxHighlighting);

			// Create modified notification command.
			NotifyModified = ReactiveCommand.CreateAsyncTask(async _ =>
			{
				// Wait until the app is foreground, dispatch back to main thread.
				await _isForeground.WhenAnyValue(x => x.IsAppForeground)
					.StartWith(_isForeground.IsAppForeground)
					.Where(x => x == true)
					.ObserveOn(RxApp.MainThreadScheduler)
					.FirstAsync();

				// We should be now foreground and on the main thread, so we can post the notification.
				await ShowChangeNotification();

				return Unit.Default;
			});

			NotifyModified.ThrownExceptions.Subscribe(ex =>
			{
				// Catch exceptions. Should not happen.
				Debug.WriteLine("Exception thrown during file change notification.");
				Debug.WriteLine(ex);
			});

			// Create a new watcher when the filename changes.
			this.WhenAnyValue(x => x.FilePath)
				.Select(f => CreateWatcher(f))
				.ToProperty(this, x => x.Watcher, out _watcher);

			// Dispose previous watcher when setting a new one.
			this.ObservableForProperty(x => x.Watcher, beforeChange: true)
				.Subscribe(change => change.GetValue()?.Dispose());

			// Notify the user when the file changes.
			this.WhenAny(x => x.Watcher, x => GetWatcherChanged(x.Value))
				.Switch()
				.Where(_ => NotifyModified.CanExecute(null))
				.SelectMany(_ => NotifyModified.ExecuteAsync())
				.Subscribe();
		}

		// Create a document backed by a file.
		public DocumentViewModel(OpenDocumentSetViewModel openDocumentSet, string path)
			: this(openDocumentSet)
		{
			FilePath = path;

			Close = CommandUtil.Create(_ => _openDocumentSet.CloseDocument(this));

			// Load the contents of the file into memory.
			LoadContents();
		}

		// (Re)load the contents of the file.
		public void LoadContents()
		{
			// TODO: Async?
			Contents = File.ReadAllText(FilePath);

			// Contents now match the file's, so clear dirty flag.
			IsDirty = false;
		}

		// Save the file to disk.
		public bool Save()
		{
			if (!string.IsNullOrEmpty(FilePath))
			{
				// Disable file change notifications -- don't want to reload if we saved it ourselves!
				Watcher.EnableRaisingEvents = false;

				// Write contents of file to disk.
				// TODO: Async?
				File.WriteAllText(FilePath, Contents);
				IsDirty = false;

				// Add to recent file list.
				_openDocumentSet.WorkspaceVM.Workspace.UserSettings.RecentFiles.AddFile(FilePath);
				_openDocumentSet.WorkspaceVM.Workspace.UserSettings.Save();

				// Re-enable the watcher.
				Watcher.EnableRaisingEvents = true;
				return true;
			}
			else
			{
				return SaveAs();
			}
		}

		// Prompt the use for a new filename under which to save the file.
		public bool SaveAs()
		{
			var dialog = new SaveFileDialog();
			dialog.Filter = "All files|*.*";

			if (!string.IsNullOrEmpty(FilePath))
			{
				dialog.InitialDirectory = Path.GetDirectoryName(FilePath);
			}

			var result = dialog.ShowDialog();
			if (result == true)
			{
				FilePath = dialog.FileName;
				return Save();
			}

			return false;
		}

		public void Dispose()
		{
			Watcher?.Dispose();
		}

		// Name to display on the document tab.
		private ObservableAsPropertyHelper<string> _displayName;
		public string DisplayName => _displayName.Value;
		
		// Path of the document on disk.
		private string _filePath;
		public string FilePath
		{
			get { return _filePath; }
			private set { this.RaiseAndSetIfChanged(ref _filePath, value); }
		}

		// Construct the name to display on the document tab.
		private static string GetDisplayName(string filename, bool isDirty)
		{
			var result = !string.IsNullOrEmpty(filename) ? Path.GetFileName(filename) : "New file";

			// Add asterisk to dirty files.
			if (isDirty)
			{
				result += "*";
			}

			return result;
		}

		private static FileSystemWatcher CreateWatcher(string filename)
		{
			if (!string.IsNullOrEmpty(filename))
			{
				var watcher = new FileSystemWatcher(Path.GetDirectoryName(filename), Path.GetFileName(filename));
				watcher.NotifyFilter = NotifyFilters.LastWrite;
				watcher.IncludeSubdirectories = false;
				watcher.EnableRaisingEvents = true;
				return watcher;
			}
			return null;
		}

		private IObservable<EventPattern<FileSystemEventArgs>> GetWatcherChanged(FileSystemWatcher watcher)
		{
			if (watcher != null)
			{
				return Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
					x => watcher.Changed += x, x => watcher.Changed -= x);
			}
			return Observable.Never<EventPattern<FileSystemEventArgs>>();
		}

		// Show a notification to the user that the file has changed.
		private async Task ShowChangeNotification()
		{
			// Prompt to reload.
			var result = await _userPrompt.ShowYesNo(
				$"{Path.GetFileName(FilePath)} was modified by an external program. Would you like to reload it?");
			if (result == UserPromptResult.Yes)
			{
				LoadContents();
			}
		}

		// Contents of the document.
		public string Contents
		{
			get { return Document.Text; }
			private set { Document.Text = value; }
		}

		// AvalonEdit document object.
		public TextDocument Document { get; }

		private bool _bDirty = false;
		public bool IsDirty
		{
			get { return _bDirty; }
			private set { this.RaiseAndSetIfChanged(ref _bDirty, value); }
		}

		public bool IsScript => Path.GetExtension(FilePath).ToLowerInvariant() == ".py";
		
		// Position of caret in the editor.
		public int CaretPosition { get; set; }

		// Horizontal and vertical scroll positions.
		public double HorizontalScrollPosition { get; set; }
		public double VerticalScrollPosition { get; set; }

		// Start point and length of selection.
		public int SelectionStart { get; set; }
		public int SelectionLength { get; set; }

		private ObservableAsPropertyHelper<IHighlightingDefinition> _syntaxHighlighting;
		public IHighlightingDefinition SyntaxHighlighting => _syntaxHighlighting.Value;

		// Back-pointer to the open document set we're in.
		private readonly OpenDocumentSetViewModel _openDocumentSet;

		// Watcher to look for external modifications.
		private ObservableAsPropertyHelper<FileSystemWatcher> _watcher;
		private FileSystemWatcher Watcher => _watcher.Value;

		private readonly IIsForegroundService _isForeground;
		private readonly IUserPrompt _userPrompt;

		// Command to close this document.
		public ReactiveCommand<object> Close { get; }

		// Command to notify the user about the document being externally modified.
		private ReactiveCommand<Unit> NotifyModified { get; }
	}
}
