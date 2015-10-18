using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using SRPCommon.Util;
using ReactiveUI;

namespace ShaderEditorApp.Projects
{
	// A sub-folder in a project tree.
	public class ProjectFolder
	{
		// Create a new empty folder.
		public ProjectFolder(ProjectFolder parent, Project project, string name)
		{
			this.parent = parent;
			Project = project;
			Name = name;

			subfolders = new ReactiveList<ProjectFolder>();
			items = new ReactiveList<ProjectItem>();
		}

		// Create a new folder form the contents of an xml element.
		public ProjectFolder(XElement element, ProjectFolder parent, Project project)
		{
			this.parent = parent;
			this.Project = project;

			// Get name.
			SerialisationUtils.ParseAttribute(element, "name", str => Name = str);

			// Create sub-folder items.
			subfolders = new ReactiveList<ProjectFolder>(
				from subfolder in element.Elements("folder")
				select new ProjectFolder(subfolder, this, project)
				);

			// Create included items.
			items = new ReactiveList<ProjectItem>(
				from item in element.Elements("include")
				select ProjectItem.LoadFromElement(item, this)
				);
		}

		// Save the folder to an XElement.
		public XElement Save()
		{
			var element = new XElement("folder",
				from subfolder in subfolders select subfolder.Save(),
				from item in items select item.Save());
			element.SetAttributeValue("name", Name);
			return element;
		}

		// Add an existing file to the project.
		public ProjectItem AddItem(string path)
		{
			// Create a new item object and add to the list.
			var item = new ProjectItem(path, this);
			items.Add(item);

			// Mark the project as dirty.
			Project.IsDirty = true;

			return item;
		}

		// Add a new sub folder.
		public void AddFolder(string name)
		{
			subfolders.Add(new ProjectFolder(this, Project, name));
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
			Debug.Assert(subfolders.Contains(child));
			subfolders.Remove(child);
			Project.IsDirty = true;
		}
		internal void RemoveItem(ProjectItem item)
		{
			Debug.Assert(items.Contains(item));
			items.Remove(item);
			Project.IsDirty = true;
		}

		// Add raw item. Used for moving.
		internal void AddItem(ProjectItem item)
		{
			items.Add(item);
			Project.IsDirty = true;
		}

		// Add raw folder. Used for moving.
		internal void AddFolder(ProjectFolder folder)
		{
			subfolders.Add(folder);
			Project.IsDirty = true;
		}

		// Can this folder be moved to the given folder?
		public bool CanMoveTo(ProjectFolder dest)
			=> !IsRoot						// Can't move the root.
				&& this != dest             // Can't move to outselves.
				&& parent != dest           // Can't move if we're already in it.
				&& !dest.IsChildOf(this);   // Can't move into one of our children.

		// Move the folder to the given target folder.
		public void MoveTo(ProjectFolder dest)
		{
			Debug.Assert(CanMoveTo(dest));

			parent.RemoveFolder(this);
			dest.AddFolder(this);
			parent = dest;
		}

		public string Name { get; set; }

		// The path within the projects internal folder structure.
		public object InternalPath => parent != null ? (parent.InternalPath + "/" + Name) : "";

		// All items in this folder, including those in sub-folders.
		public IEnumerable<ProjectItem> AllItems
		{
			get
			{
				var subItems = from subfolder in subfolders from item in subfolder.AllItems select item;
				return subItems.Concat(items);
			}
		}

		// Is this folder the root?
		public bool IsRoot => parent == null;

		// Public is this folder a child of the given folder.
		public bool IsChildOf(ProjectFolder other)
		{
			var test = parent;
			while (test != null)
			{
				if (test == other)
				{
					return true;
				}
				test = test.parent;
			}
			return false;
		}

		// Read-only mirrors of the sub-folder and item lists.
		public IReadOnlyReactiveList<ProjectFolder> SubFolders => subfolders;
		public IReadOnlyReactiveList<ProjectItem> Items => items;

		// Lists containing our sub-folders and items.
		private ReactiveList<ProjectFolder> subfolders;
		private ReactiveList<ProjectItem> items;

		internal Project Project { get; }
		private ProjectFolder parent;
	}
}
