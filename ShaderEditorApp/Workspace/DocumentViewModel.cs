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

namespace ShaderEditorApp.Workspace
{
	// ViewModel class representing an open document.
	public class DocumentViewModel : ViewModelBase
	{
		// Create a new (empty) document.
		public DocumentViewModel(WorkspaceViewModel workspace)
		{
			this.workspace = workspace;
			Document = new TextDocument();

			// Dirty when document changes.
			Document.TextChanged += (o, e) => IsDirty = true;
		}

		// Create a document backed by a file.
		public DocumentViewModel(WorkspaceViewModel workspace, string path)
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
		public void Save()
		{
			if (!String.IsNullOrEmpty(FilePath))
			{
				// Disable file change notifications -- don't want to reload if we saved it ourselves!
				watcher.EnableRaisingEvents = false;

				// Write contents of file to disk.
				// TODO: Async?
				File.WriteAllText(FilePath, Contents);
				IsDirty = false;

				// Re-enable the watcher.
				watcher.EnableRaisingEvents = true;
			}
			else
			{
				SaveAs();
			}
		}

		// Prompt the use for a new filename under which to save the file.
		public void SaveAs()
		{
			var dialog = new SaveFileDialog();
			dialog.Filter = "All files|*.*";

			if (!String.IsNullOrEmpty(FilePath))
				dialog.InitialDirectory = Path.GetDirectoryName(FilePath);

			var result = dialog.ShowDialog();
			if (result == true)
			{
				FilePath = dialog.FileName;
				Save();
			}
		}

		protected override void OnDispose()
		{
			base.OnDispose();

			if (watcher != null)
			{
				watcher.Changed -= FileChanged;
				watcher.Dispose();
				watcher = null;
			}
		}

		// Name of the file.
		public override string DisplayName
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
		private string filePath_;
		public string FilePath
		{
			get { return filePath_; }
			private set
			{
				if (value != filePath_)
				{
					filePath_ = value;

					// (Re-)create the file watcher.
					if (watcher != null)
					{
						watcher.Dispose();
					}

					watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath_), Path.GetFileName(filePath_));
					watcher.NotifyFilter = NotifyFilters.LastWrite;
					watcher.IncludeSubdirectories = false;
					watcher.Changed += FileChanged;
					watcher.EnableRaisingEvents = true;

					OnPropertyChanged();
					OnPropertyChanged("DisplayName");
				}
			}
		}

		void FileChanged(object sender, FileSystemEventArgs e)
		{
			workspace.AddModifiedDocument(this);
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
					OnPropertyChanged();
					OnPropertyChanged("DisplayName");
				}
			}
		}

		// Position of caret in the editor.
		public int CaretPosition { get; set; }

		// Horizontal and vertical scroll positions.
		public double HorizontalScrollPosition { get; set; }
		public double VerticalScrollPosition { get; set; }

		// Start point and length of selection.
		public int SelectionStart { get; set; }
		public int SelectionLength { get; set; }

		// Back-pointer to the workspace we're in.
		private readonly WorkspaceViewModel workspace;

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
					closeCmd = new RelayCommand(param => workspace.CloseDocument(this));
				}
				return closeCmd;
			}
		}
	}
}
