using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace ShaderEditorApp.Projects
{
	// A sub-folder in a project tree.
	public class ProjectFolder
	{
		// Internal-only constructor.
		internal ProjectFolder(ProjectFolder parent, Project project)
		{
			this.parent = parent;
			this.Project = project;
		}

		// Load the contents of the folder from disk.
		public static ProjectFolder LoadFromElement(XElement element, ProjectFolder parent, Project project)
		{
			ProjectFolder result = new ProjectFolder(parent, project);

			// Get name.
			SerialisationUtils.ParseAttribute(element, "name", str => result.Name = str);

			// Create sub-folder items.
			result.folders = new ObservableCollection<ProjectFolder>(
				from subfolder in element.Elements("folder")
				select ProjectFolder.LoadFromElement(subfolder, result, project)
				);

			// Create included items.
			result.items = new ObservableCollection<ProjectItem>(
				from item in element.Elements("include")
				select ProjectItem.LoadFromElement(item, result)
				);

			return result;
		}

		// Save the folder to an XElement.
		public XElement Save()
		{
			var element = new XElement("folder",
				from subfolder in folders select subfolder.Save(),
				from item in items select item.Save());
			element.SetAttributeValue("name", Name);
			return element;
		}

		// Add an existing file to the project.
		public void AddItem(string path)
		{
			// Create a new item object and add to the list.
			items.Add(new ProjectItem(path, this));

			// Mark the project as dirty.
			Project.IsDirty = true;
		}

		// Remove this folder from the project.
		public void RemoveFromProject()
		{
			if (parent == null)
				throw new InvalidOperationException("Cannot remove the root node.");

			parent.RemoveFolder(this);
		}

		// Internal methods to remove a subfolder or item from the folder.
		internal void RemoveFolder(ProjectFolder child)
		{
			Debug.Assert(folders.Contains(child));
			folders.Remove(child);
		}
		internal void RemoveItem(ProjectItem item)
		{
			Debug.Assert(items.Contains(item));
			items.Remove(item);
		}

		public string Name { get; set; }

		// All items in this folder, including those in sub-folders.
		public IEnumerable<ProjectItem> AllItems
		{
			get
			{
				var subItems = from subfolder in folders from item in subfolder.AllItems select item;
				return subItems.Concat(items);
			}
		}

		// Read-only mirrors of the sub-folder and item lists.
		public ReadOnlyObservableCollection<ProjectFolder> SubFolders { get { return new ReadOnlyObservableCollection<ProjectFolder>(folders); } }
		public ReadOnlyObservableCollection<ProjectItem> Items { get { return new ReadOnlyObservableCollection<ProjectItem>(items); } }

		// Lists containing our sub-folders and items.
		private ObservableCollection<ProjectFolder> folders;
		private ObservableCollection<ProjectItem> items;

		internal Project Project { get; private set; }
		private ProjectFolder parent;
	}
}
