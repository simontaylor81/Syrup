using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ShaderEditorApp.MVVMUtil;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.Win32;
using System.Windows;
using ReactiveUI;
using ShaderEditorApp.Interfaces;
using System.Reactive.Linq;
using System.Diagnostics;
using Splat;
using System.Reactive;

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
		}

		// Create a document backed by a file.
		public DocumentViewModel(OpenDocumentSetViewModel openDocumentSet, string path)
			: this(openDocumentSet)
		{
			FilePath = path;

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
				_watcher.EnableRaisingEvents = false;

				// Write contents of file to disk.
				// TODO: Async?
				File.WriteAllText(FilePath, Contents);
				IsDirty = false;

				// Add to recent file list.
				_openDocumentSet.WorkspaceVM.Workspace.UserSettings.RecentFiles.AddFile(FilePath);
				_openDocumentSet.WorkspaceVM.Workspace.UserSettings.Save();

				// Re-enable the watcher.
				_watcher.EnableRaisingEvents = true;
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
			if (_watcher != null)
			{
				_watcherSubscription.Dispose();
				_watcher.Dispose();
				_watcher = null;
			}
		}

		// Name of the file.
		public string DisplayName
		{
			get
			{
				var result = Path.GetFileName(FilePath);

				// Add asterisk to dirty files.
				if (IsDirty)
					result += "*";

				return result;
			}
		}
		
		// Path of the document on disk.
		private string _filePath;
		public string FilePath
		{
			get { return _filePath; }
			private set
			{
				if (value != _filePath)
				{
					_filePath = value;

					// (Re-)create the file watcher.
					if (_watcher != null)
					{
						_watcherSubscription.Dispose();
						_watcher.Dispose();
					}

					_watcher = new FileSystemWatcher(Path.GetDirectoryName(_filePath), Path.GetFileName(_filePath));
					_watcher.NotifyFilter = NotifyFilters.LastWrite;
					_watcher.IncludeSubdirectories = false;
					SubscribeToWatcher();
					_watcher.EnableRaisingEvents = true;

					this.RaisePropertyChanged();
					this.RaisePropertyChanged(nameof(DisplayName));
				}
			}
		}

		private void SubscribeToWatcher()
		{
			// Convert notifications to observable.
			var fileChanged = Observable.FromEventPattern(_watcher, nameof(_watcher.Changed));

			_watcherSubscription = fileChanged
				// FileSystemWatcher can send multiple events for the same conceptual operation,
				// so we disable notifications whilst we're already showing one.
				// I'm sure there's a more elegant way of doing this in Rx, but this does the job.
				.Where(_ => !_disableModificationNotifications)
				.Do(_ => _disableModificationNotifications = true)
				// Each time we get a changed, wait until the app is foreground,
				// dispatch back to main thread.
				.SelectMany(_ => _isForeground.WhenAnyValue(x => x.IsAppForeground)
					.StartWith(_isForeground.IsAppForeground)
					.Where(x => x == true)
					.ObserveOn(RxApp.MainThreadScheduler)
					.FirstAsync())
				// User SelectMany as a kind of 'SubscribeAsync'.
				.SelectMany(async _ =>
				{
					// We should be now foreground and on the main thread, so we can post the notification.
					await ShowChangeNotification();

					// Re-enable notifications now that we're done.
					_disableModificationNotifications = false;
					return Unit.Default;
				})
				.Subscribe(_ => { }, ex =>
				{
					// Catch exceptions. Should not happen.
					Debug.WriteLine("Exception thrown during file change notification.");
					Debug.WriteLine(ex);
				});
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

		private bool bDirty_ = false;
		public bool IsDirty
		{
			get { return bDirty_; }
			private set
			{
				if (value != bDirty_)
				{
					bDirty_ = value;
					this.RaisePropertyChanged();
					this.RaisePropertyChanged(nameof(DisplayName));
				}
			}
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

		// Back-pointer to the open document set we're in.
		private readonly OpenDocumentSetViewModel _openDocumentSet;

		// Watcher to look for external modifications.
		private FileSystemWatcher _watcher;

		// Subscription to watcher event.
		private IDisposable _watcherSubscription;

		// Switch to disable watcher notifications whilst we're displaying the message box.
		private bool _disableModificationNotifications = false;

		private readonly IIsForegroundService _isForeground;
		private readonly IUserPrompt _userPrompt;

		// Command to close this document.
		private RelayCommand closeCmd;

		public ICommand CloseCmd
		{
			get
			{
				if (closeCmd == null)
				{
					closeCmd = new RelayCommand(param => _openDocumentSet.CloseDocument(this));
				}
				return closeCmd;
			}
		}
	}
}
