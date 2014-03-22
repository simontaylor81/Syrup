using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;

namespace ShaderEditorApp.Projects
{
	// A collection of files that makes up a project.
	public class Project
	{
		// Load an existing project from disk.
		public static Project LoadFromFile(string filename)
		{
			Project result = new Project();
			result.filename = filename;

			// Load the xml file.
			var xdoc = XDocument.Load(filename);

			// Load the root folder.
			result.RootFolder = ProjectFolder.LoadFromElement(xdoc.Root, null, result);

			// Read list of open documents, if present.
			var openDocsElement = xdoc.Descendants("OpenDocuments").FirstOrDefault();
			if (openDocsElement != null)
				result.savedOpenDocuments = (from doc in openDocsElement.Descendants("OpenDocument")
											 select doc.Attribute("path").Value).ToArray();

			return result;
		}

		// Create an empty project, and write it out to the given filename.
		public static Project CreateNew(string filename)
		{
			// Create empty project.
			Project result = new Project();
			result.filename = filename;

			// Create new empty root folder.
			result.RootFolder = new ProjectFolder(null, result);

			// Write out to disk.
			result.Save();

			return result;
		}

		public void Save()
		{
			// Root element *is* the root folder.
			var rootElement = RootFolder.Save();
			rootElement.Name = "project";

			// Add open documents.
			rootElement.Add(new XElement("OpenDocuments", from doc in savedOpenDocuments select GetOpenDocumentElement(doc)));

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
		public string Name { get { return Path.GetFileNameWithoutExtension(filename); } }
		public string BasePath { get { return Path.GetDirectoryName(filename); } }
		
		// Has the project changed since it was last saved?
		public bool IsDirty
		{
			get { return bDirty; }
			internal set
			{
				if (value != bDirty)
				{
					bDirty = value;
					if (null != DirtyChanged)
						DirtyChanged();
				}
			}
		}
		public event Action DirtyChanged;

		public IEnumerable<ProjectItem> AllItems { get { return RootFolder.AllItems; } }

		// List of scripts to be executed at startup.
		public IEnumerable<string> StartupScripts
		{
			get
			{
				return from item in AllItems where item.RunAtStartup && item.IsScript select item.AbsolutePath;
			}
		}

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
