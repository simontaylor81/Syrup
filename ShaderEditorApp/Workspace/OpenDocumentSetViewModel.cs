﻿using Microsoft.Win32;
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
			_workspaceVM = workspaceVM;

			// Create documents list, and wrap in a read-only wrapper.
			documents = new ObservableCollection<DocumentViewModel>();
			Documents = new ReadOnlyObservableCollection<DocumentViewModel>(documents);

			Application.Current.Activated += (o, _e) => isAppForeground = true;
			Application.Current.Deactivated += (o, _e) => isAppForeground = false;

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

		public void Tick()
		{
			CheckModifiedDocuments();
		}

		public void OpenDocument(string path, bool bReload)
		{
			// Look for an already open document.
			var document = documents.FirstOrDefault(doc => doc.FilePath == path);
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
			}

			// Make active document.
			_workspaceVM.ActiveWindow = document;
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
			else if (_workspaceVM.Workspace.Project != null)
			{
				dialog.InitialDirectory = _workspaceVM.Workspace.Project.BasePath;
			}

			var result = dialog.ShowDialog();
			if (result == true)
			{
				// Force a reload if the document is already loaded.
				OpenDocument(dialog.FileName, true);
			}
		}

		// Create a new document.
		public void NewDocument()
		{
			var document = new DocumentViewModel(this);
			documents.Add(document);
			_workspaceVM.ActiveWindow = document;
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

		// Notification that a document has been modified, and might need to be reloaded. Thread-safe.
		internal void AddModifiedDocument(DocumentViewModel document)
		{
			lock (modifiedDocuments)
			{
				modifiedDocuments.Add(document);
			}
		}

		// If there are any externally-modified files, prompt to reload them.
		private void CheckModifiedDocuments()
		{
			// Don't do anything if the app's not in the foreground.
			if (!isAppForeground)
				return;

			DocumentViewModel[] docsToReload;
			lock (modifiedDocuments)
			{
				docsToReload = modifiedDocuments.ToArray();
				modifiedDocuments.Clear();
			}

			foreach (var document in docsToReload)
			{
				// Prompt to reload.
				var result = MessageBox.Show(
					string.Format("{0} was modified by an external program. Would you like to reload it?", Path.GetFileName(document.FilePath)),
					"SRP", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
				{
					document.LoadContents();
				}
			}
		}

		private ObservableCollection<DocumentViewModel> documents;
		public ReadOnlyObservableCollection<DocumentViewModel> Documents { get; }

		// Property that tracks the currently active document.
		private ObservableAsPropertyHelper<DocumentViewModel> _activeDocument;
		public DocumentViewModel ActiveDocument => _activeDocument.Value;

		// List of documents that have been externally modified.
		private HashSet<DocumentViewModel> modifiedDocuments = new HashSet<DocumentViewModel>();

		private readonly WorkspaceViewModel _workspaceVM;
		private bool isAppForeground;
	}
}