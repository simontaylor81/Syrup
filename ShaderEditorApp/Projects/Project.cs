using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Reactive.Subjects;

namespace ShaderEditorApp.Projects
{
	// A collection of files that makes up a project.
	public class Project
	{
		// Load an existing project from disk.
		public static Project LoadFromFile(string filename)
		{
			var result = new Project();
			result.filename = filename;

			// Load the xml file.
			var xdoc = XDocument.Load(filename);

			// Load the root folder.
			var rootFolderElement = xdoc.Root.Elements("folder").FirstOrDefault();
			if (rootFolderElement != null)
			{
				result.RootFolder = new ProjectFolder(rootFolderElement, null, result);
			}
			else
			{
				// Root folder node missing -- create empty one.
				result.RootFolder = new ProjectFolder(null, result, "root");
			}

			// Read list of open documents, if present.
			var openDocsElement = xdoc.Root.Elements("OpenDocuments").FirstOrDefault();
			if (openDocsElement != null)
				result.savedOpenDocuments = (from doc in openDocsElement.Elements("OpenDocument")
											 select doc.Attribute("path").Value).ToArray();

			// Try to get the default scene.
			var defaultSceneAttr = xdoc.Root.Attribute("DefaultScene");
			if (defaultSceneAttr != null)
			{
				result.DefaultScene = result.FindByInternalPath(defaultSceneAttr.Value);
			}

			return result;
		}

		// Create an empty project, and write it out to the given filename.
		public static Project CreateNew(string filename)
		{
			// Create empty project.
			var result = new Project();
			result.filename = filename;

			// Create new empty root folder.
			result.RootFolder = new ProjectFolder(null, result, "root");

			// Write out to disk.
			result.Save();

			return result;
		}

		public void Save()
		{
			// Create root element to contain everything.
			var rootElement = new XElement("project");

			// Add the root folder.
			rootElement.Add(RootFolder.Save());

			// Add open documents.
			rootElement.Add(new XElement("OpenDocuments", from doc in savedOpenDocuments select GetOpenDocumentElement(doc)));

			// Add the default scene.
			if (DefaultScene != null)
			{
				rootElement.Add(new XAttribute("DefaultScene", DefaultScene.InternalPath));
			}

			// Create XDocument and save to disk.
			var xdoc = new XDocument(rootElement);
			xdoc.Save(filename);

			IsDirty = false;
		}

		// Create an XElement object for an open document entry, for saving.
		private XElement GetOpenDocumentElement(string path)
		{
			var element = new XElement("OpenDocument");
			element.SetAttributeValue("path", path);
			return element;
		}

		// Hide constructor -- use LoadFromFile or CreateNew instead.
		private Project()
		{
		}

		// Public properties.
		public string FilePath => filename;
		public string Name => Path.GetFileNameWithoutExtension(filename);
		public string BasePath => Path.GetDirectoryName(filename);

		// Has the project changed since it was last saved?
		public bool IsDirty
		{
			get { return bDirty; }
			internal set
			{
				if (value != bDirty)
				{
					bDirty = value;
					_isDirtyObservable.OnNext(bDirty);
				}
			}
		}
		private Subject<bool> _isDirtyObservable = new Subject<bool>();
		public IObservable<bool> IsDirtyObservable => _isDirtyObservable;

		public IEnumerable<ProjectItem> AllItems => RootFolder.AllItems;

		// Find a project item based on an internal path.
		public ProjectItem FindByInternalPath(string path)
		{
			var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0)
			{
				// Must have at least a filename.
				return null;
			}

			var folder = RootFolder;

			// Descend through folder structure (last part is the item name).
			for (int i = 0; i < parts.Length - 1; i++)
			{
				folder = folder.SubFolders.FirstOrDefault(sub => sub.Name == parts[i]);
				if (folder == null)
				{
					// Sub-folder not found.
					return null;
				}
			}

			// Find the item in the folder.
			return folder.Items.FirstOrDefault(item => item.Name == parts.Last());
		}

		// List of scripts to be executed at startup.
		public IEnumerable<string> StartupScripts
			=> from item in AllItems
			   where item.RunAtStartup && item.Type == ProjectItemType.Script
			   select item.AbsolutePath;

		// ProjectItem for the default scene.
		public ProjectItem DefaultScene
		{
			get { return _defaultScene; }
			internal set
			{
				if (value != _defaultScene)
				{
					_defaultScene = value;
					_defaultSceneChanged.OnNext(value);
				}
			}
		}

		// event fired when the default scene changes.
		public IObservable<ProjectItem> DefaultSceneChanged => _defaultSceneChanged;
		private Subject<ProjectItem> _defaultSceneChanged = new Subject<ProjectItem>();

		private ProjectItem _defaultScene;

		// Saved set of paths to open documents. Don't have to be in the project itself.
		// Does not actively track open documents in workspace. Should be set before saving.
		public IEnumerable<string> SavedOpenDocuments
		{
			get { return savedOpenDocuments; }
			set { savedOpenDocuments = value.ToArray(); }
		}
		private string[] savedOpenDocuments = new string[0];

		private string filename;
		private bool bDirty = false;

		public ProjectFolder RootFolder { get; private set; }
	}
}
