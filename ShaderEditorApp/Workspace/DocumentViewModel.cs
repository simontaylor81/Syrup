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

namespace ShaderEditorApp.ViewModel
{
	// ViewModel class representing an open document.
	public class DocumentViewModel : ReactiveObject, IDisposable
	{
		// Create a new (empty) document.
		public DocumentViewModel(OpenDocumentSetViewModel openDocumentSet)
		{
			_openDocumentSet = openDocumentSet;
			Document = new TextDocument();

			// Dirty when document changes.
			Document.TextChanged += (o, e) => IsDirty = true;
		}

		// Create a document backed by a file.
		public DocumentViewModel(OpenDocumentSetViewModel workspace, string path)
			: this(workspace)
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
				watcher.EnableRaisingEvents = false;

				// Write contents of file to disk.
				// TODO: Async?
				File.WriteAllText(FilePath, Contents);
				IsDirty = false;

				// Re-enable the watcher.
				watcher.EnableRaisingEvents = true;
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
			if (watcher != null)
			{
				watcher.Changed -= FileChanged;
				watcher.Dispose();
				watcher = null;
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
					if (watcher != null)
					{
						watcher.Dispose();
					}

					watcher = new FileSystemWatcher(Path.GetDirectoryName(_filePath), Path.GetFileName(_filePath));
					watcher.NotifyFilter = NotifyFilters.LastWrite;
					watcher.IncludeSubdirectories = false;
					watcher.Changed += FileChanged;
					watcher.EnableRaisingEvents = true;

					this.RaisePropertyChanged();
					this.RaisePropertyChanged(nameof(DisplayName));
				}
			}
		}

		void FileChanged(object sender, FileSystemEventArgs e)
		{
			_openDocumentSet.AddModifiedDocument(this);
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
		private FileSystemWatcher watcher;

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
