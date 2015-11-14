using Microsoft.Win32;
using ReactiveUI;
using SRPCommon.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShaderEditorApp.ViewModel
{
	// View Model class for managing the set of open documents.
	public class OpenDocumentSetViewModel : ReactiveObject
	{
		public OpenDocumentSetViewModel(WorkspaceViewModel workspaceVM)
		{
			WorkspaceVM = workspaceVM;

			// Create documents list, and wrap in a read-only wrapper.
			documents = new ObservableCollection<DocumentViewModel>();
			Documents = new ReadOnlyObservableCollection<DocumentViewModel>(documents);

			// Active document is just the most recent active window that was a document.
			_activeDocument = workspaceVM.WhenAnyValue(x => x.ActiveWindow)
				.OfType<DocumentViewModel>()
				.ToProperty(this, x => x.ActiveDocument);

			// When the project changes...
			workspaceVM.Workspace.WhenAnyValue(x => x.Project).Subscribe(project =>
			{
				// Close existing documents.
				CloseAllDocuments();

				// And re-open documents that were open last time.
				if (project != null)
				{
					foreach (var file in project.SavedOpenDocuments)
					{
						OpenDocument(file, false);
					}
				}
			});
		}

		public void OpenDocument(string path, bool bReload)
		{
			// Look for an already open document.
			var document = documents.FirstOrDefault(doc => PathUtils.PathsEqual(doc.FilePath, path));
			if (document != null)
			{
				// Force reload if required.
				if (bReload)
					document.LoadContents();
			}
			else if (File.Exists(path))
			{
				// Create a new document.
				document = new DocumentViewModel(this, path);
				documents.Add(document);
			}
			else
			{
				OutputLogger.Instance.LogLine(LogCategory.Log, "File not found: " + path);
				return;
			}

			// Make active document.
			WorkspaceVM.ActiveWindow = document;
		}

		// Open a document by asking the user for a file to open.
		public void OpenDocumentPrompt()
		{
			var dialog = new OpenFileDialog();
			dialog.Filter = "All files|*.*";

			// Put initial directory in the same place as the current document, if there is one.
			if (ActiveDocument != null && !String.IsNullOrEmpty(ActiveDocument.FilePath))
			{
				dialog.InitialDirectory = Path.GetDirectoryName(ActiveDocument.FilePath);
			}
			else if (WorkspaceVM.Workspace.Project != null)
			{
				dialog.InitialDirectory = WorkspaceVM.Workspace.Project.BasePath;
			}

			var result = dialog.ShowDialog();
			if (result == true)
			{
				// Force a reload if the document is already loaded.
				OpenDocument(dialog.FileName, true);

				// Add to recent file list.
				// Only do this for explicit open operations otherwise the list gets
				// swamped just opening a project.
				WorkspaceVM.Workspace.UserSettings.RecentFiles.AddFile(dialog.FileName);
				WorkspaceVM.Workspace.UserSettings.Save();
			}
		}

		// Create a new document.
		public void NewDocument()
		{
			var document = new DocumentViewModel(this);
			documents.Add(document);
			WorkspaceVM.ActiveWindow = document;
		}

		public void CloseDocument(DocumentViewModel document)
		{
			document.Dispose();
			documents.Remove(document);
		}

		public void CloseAllDocuments()
		{
			DisposableUtil.DisposeList(documents);
		}

		// Save all dirty documents.
		public bool SaveAllDirty()
		{
			var result = true;

			foreach (var document in Documents)
			{
				if (document.IsDirty)
				{
					result = document.Save() & result;
				}
			}

			return result;
		}

		private ObservableCollection<DocumentViewModel> documents;
		public ReadOnlyObservableCollection<DocumentViewModel> Documents { get; }

		// Property that tracks the currently active document.
		private ObservableAsPropertyHelper<DocumentViewModel> _activeDocument;
		public DocumentViewModel ActiveDocument => _activeDocument.Value;

		public WorkspaceViewModel WorkspaceVM { get; }
	}
}
