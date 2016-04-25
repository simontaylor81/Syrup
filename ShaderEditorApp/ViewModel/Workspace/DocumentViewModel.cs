﻿using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.CodeAnalysis;
using Microsoft.Win32;
using ReactiveUI;
using ShaderEditorApp.Interfaces;
using ShaderEditorApp.Model.Editor;
using ShaderEditorApp.Model.Editor.CSharp;
using ShaderEditorApp.MVVMUtil;
using Splat;

using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace ShaderEditorApp.ViewModel.Workspace
{
	// ViewModel class representing an open document.
	public class DocumentViewModel : ReactiveObject, IDisposable
	{
		// Create a new (empty) document.
		public static DocumentViewModel CreateEmpty(
			IUserPrompt userPrompt = null,
			IIsForegroundService isForeground = null,
			IUserSettings userSettings = null)
		{
			return new DocumentViewModel(null, null, userPrompt, isForeground, userSettings);
		}

		// Create a document backed by a file.
		public static DocumentViewModel CreateFromFile(
			string path,
			IUserPrompt userPrompt = null,
			IIsForegroundService isForeground = null,
			IUserSettings userSettings = null)
		{
			// TODO: Async?
			var contents = File.ReadAllText(path);

			return new DocumentViewModel(contents, path, userPrompt, isForeground, userSettings);
		}

		private DocumentViewModel(
			string contents,
			string filePath,
			IUserPrompt userPrompt,
			IIsForegroundService isForeground,
			IUserSettings userSettings)
		{
			FilePath = filePath;

			_userPrompt = userPrompt ?? Locator.Current.GetService<IUserPrompt>();
			isForeground = isForeground ?? Locator.Current.GetService<IIsForegroundService>();
			_userSettings = userSettings ?? Locator.Current.GetService<IUserSettings>();

			Document = new TextDocument(contents);

			// Create a text source container for this file.
			// TEMP: Currently doing this for all files, not just C#.
			_sourceTextContainer = new DocumentSourceTextContainer(Document);
			_editorServices = new RoslynDocumentServices(_sourceTextContainer, FilePath);
			_completionService = new CompletionService(_editorServices);

			// Get 'dirtiness' from the document's undo stack.
			_isDirty = this.WhenAny(x => x.Document.UndoStack.IsOriginalFile, change => !change.Value)
				.ToProperty(this, x => x.IsDirty);

			Close = ReactiveCommand.Create();

			// Roslyn stuff. This really needs to be factored out somewhere C#-specific.
			{
				GoToDefinition = ReactiveCommand.CreateAsyncTask(_ => GoToDefinitionImpl());

				// TODO: Can diagnostics be for other files?
				GetDiagnostics = ReactiveCommand.CreateAsyncTask((_, ct) => _editorServices.GetDiagnosticsAsync(ct));
				GetDiagnostics.ToProperty(this, x => x.Diagnostics, out _diagnostics, ImmutableArray<Diagnostic>.Empty);

				// Update diagnostics when the document changes.
				var documentChanged = Observable.FromEventPattern<DocumentChangeEventArgs>(h => Document.Changed += h, h => Document.Changed -= h);
				documentChanged
					.Throttle(TimeSpan.FromMilliseconds(500))
					.InvokeCommand(GetDiagnostics);
			}

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
				await isForeground.WhenAnyValue(x => x.IsAppForeground)
					.StartWith(isForeground.IsAppForeground)
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

		// (Re)load the contents of the file.
		public void ReloadContents()
		{
			// TODO: Async?
			Document.Text = File.ReadAllText(FilePath);

			// Contents now match the file's, so clear dirty flag.
			Document.UndoStack.MarkAsOriginalFile();
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
				using (var writer = new StreamWriter(FilePath))
				{
					Document.WriteTextTo(writer);
				}

				Document.UndoStack.MarkAsOriginalFile();

				// Add to recent file list.
				_userSettings.RecentFiles.AddFile(FilePath);
				_userSettings.Save();

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

		// Get potential completions for the current position.
		public Task<CompletionList> GetCompletions(char? triggerChar)
			=> _completionService.GetCompletions(CaretOffset, triggerChar);

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
				ReloadContents();
			}
		}

		private async Task GoToDefinitionImpl()
		{
			// Use the language service to get the location of the symbol underneath the caret.
			var span = await _editorServices.FindDefinitionAsync(CaretOffset);
			if (span.HasValue)
			{
				SelectionStart = span.Value.Start;
				SelectionLength = span.Value.Length;
			}
		}

		// AvalonEdit document object.
		public TextDocument Document { get; }

		private readonly ObservableAsPropertyHelper<bool> _isDirty;
		public bool IsDirty => _isDirty.Value;

		public bool IsScript
		{
			get
			{
				var ext = Path.GetExtension(FilePath).ToLowerInvariant();
				return ext == ".py" || ext == ".cs" || ext == ".csx";
			}
		}

		// Position of caret in the editor.
		private TextLocation _caretPosition;
		public TextLocation CaretPosition
		{
			get { return _caretPosition; }
			set { this.RaiseAndSetIfChanged(ref _caretPosition, value); }
		}

		// Selection start and length (separate properties to avoid messing about with another type).
		private int _selectionStart;
		public int SelectionStart
		{
			get { return _selectionStart; }
			set { this.RaiseAndSetIfChanged(ref _selectionStart, value); }
		}
		private int _selectionLength;
		public int SelectionLength
		{
			get { return _selectionLength; }
			set { this.RaiseAndSetIfChanged(ref _selectionLength, value); }
		}

		// Offset in the document of the caret.
		public int CaretOffset => Document.GetOffset(CaretPosition);

		private ObservableAsPropertyHelper<IHighlightingDefinition> _syntaxHighlighting;
		public IHighlightingDefinition SyntaxHighlighting => _syntaxHighlighting.Value;

		// Watcher to look for external modifications.
		private ObservableAsPropertyHelper<FileSystemWatcher> _watcher;
		private FileSystemWatcher Watcher => _watcher.Value;

		private readonly IUserPrompt _userPrompt;
		private readonly IUserSettings _userSettings;
		private readonly DocumentSourceTextContainer _sourceTextContainer;
		private readonly RoslynDocumentServices _editorServices;
		private readonly CompletionService _completionService;

		public ICodeTipProvider CodeTipProvider => _editorServices;

		// Command to close this document.
		public ReactiveCommand<object> Close { get; }

		public ReactiveCommand<Unit> GoToDefinition { get; }
		public ReactiveCommand<ImmutableArray<Diagnostic>> GetDiagnostics { get; }

		private ObservableAsPropertyHelper<ImmutableArray<Diagnostic>> _diagnostics;

		public ImmutableArray<Diagnostic> Diagnostics => _diagnostics.Value;

		// Command to notify the user about the document being externally modified.
		private ReactiveCommand<Unit> NotifyModified { get; }
	}
}