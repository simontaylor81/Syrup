using System;
using System.Collections.Generic;
using System.Linq;
using ShaderEditorApp.MVVMUtil;
using SRPCommon.UserProperties;
using ShaderEditorApp.Projects;
using ReactiveUI;
using System.Reactive.Linq;
using GongSolutions.Wpf.DragDrop;
using System.Windows;

namespace ShaderEditorApp.ViewModel.Projects
{
	// View-model class for the project itself. Derives from the folder type, which is used to display the root node.
	public class ProjectViewModel : ProjectFolderViewModel, IPropertySource, IHierarchicalBrowserRootViewModel, IDropTarget
	{
		public ProjectViewModel(Project project, WorkspaceViewModel workspace)
			: base(project.RootFolder, project, workspace)
		{
			// Create save command.
			Save = CommandUtil.Create(_ => SaveImpl());

			// Can't remove the project itself, but you can save it.
			MenuItems = new object[]
			{
				new CommandMenuItem(new CommandViewModel("Save", Save)),
				new CommandMenuItem(new CommandViewModel("Add Existing File", AddExisting)),
				new CommandMenuItem(new CommandViewModel("Add New File", AddNewFile)),
				new CommandMenuItem(new CommandViewModel("Add New Scene", AddNewScene)),
				new CommandMenuItem(new CommandViewModel("Add Folder", AddSubFolder)),
			};

			// We don't have any properties of our own.
			UserProperties = null;

			_displayName = this.WhenAnyObservable(x => x.Project.IsDirtyObservable)
				.Select(isDirty => GetDisplayName(isDirty))
				.ToProperty(this, x => x.DisplayName, GetDisplayName(Project.IsDirty));

			_properties = this.WhenAnyValue(x => x.ActiveItem)
				.Select(activeItem => GetProperties(activeItem))
				.ToProperty(this, x => x.Properties);
		}

		private string GetDisplayName(bool isDirty)
		{
			var result = Project.Name;
			if (isDirty)
				result += "*";
			return result;
		}

		private IEnumerable<IUserProperty> GetProperties(IHierarchicalBrowserNodeViewModel activeItem)
		{
			if (ActiveItem != null)
				return ActiveItem.UserProperties;
			else
				return UserProperties;
		}

		// Properties to display for the currently selected property item.
		private ObservableAsPropertyHelper<IEnumerable<IUserProperty>> _properties;
		public IEnumerable<IUserProperty> Properties => _properties.Value;

		// Currently selected node in the project.
		private IHierarchicalBrowserNodeViewModel activeItem;
		public IHierarchicalBrowserNodeViewModel ActiveItem
		{
			get { return activeItem; }
			set { this.RaiseAndSetIfChanged(ref activeItem, value); }
		}

		private void SaveImpl()
		{
			// Set list of open documents so they can be restored next time.
			Project.SavedOpenDocuments = from doc in WorkspaceVM.OpenDocumentSet.Documents select doc.FilePath;

			Project.Save();
		}

		public void DragOver(IDropInfo dropInfo)
		{
			// Can only drag onto folders, not items.
			var targetFolder = dropInfo.TargetItem as ProjectFolderViewModel;
			if (targetFolder != null)
			{
				var draggedFolder = dropInfo.Data as ProjectFolderViewModel;
				var draggedItem = dropInfo.Data as ProjectItemViewModel;
				var draggedData = dropInfo.Data as IDataObject;

				bool bCanDrop = false;

				if (draggedItem != null)
				{
					// Folder items can always be dropped.
					bCanDrop = draggedItem.CanMoveTo(targetFolder);
				}
				else if (draggedFolder != null)
				{
					// Folders are complicated.
					bCanDrop = draggedFolder.CanMoveTo(targetFolder);
				}
				else if (draggedData != null)
				{
					// Are we dragging a file from explorer?
					bCanDrop = draggedData.GetDataPresent(DataFormats.FileDrop);
				}

				if (bCanDrop)
				{
					dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
					dropInfo.Effects = DragDropEffects.Move;
				}
			}
		}

		public void Drop(IDropInfo dropInfo)
		{
			var targetFolder = (ProjectFolderViewModel)dropInfo.TargetItem;

			var draggedFolder = dropInfo.Data as ProjectFolderViewModel;
			var draggedItem = dropInfo.Data as ProjectItemViewModel;
			var draggedData = dropInfo.Data as IDataObject;

			if (draggedFolder != null)
			{
				draggedFolder.MoveTo(targetFolder);
			}
			else if (draggedItem != null)
			{
				draggedItem.MoveTo(targetFolder);
			}
			else if (draggedData != null)
			{
				// Add dropped file to the project.
				var paths = (string[])draggedData.GetData(DataFormats.FileDrop);
				foreach (var path in paths)
				{
					targetFolder.AddFile(path);
				}
			}
		}


		// Allow this object to be used as a single root node of the tree (since the tree control needs a list of items).
		public IEnumerable<IHierarchicalBrowserNodeViewModel> RootNodes => new[] { this };

		// Command to save the project.
		public ReactiveCommand<object> Save { get; }
	}
}
