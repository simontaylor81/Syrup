﻿using System;
using System.IO;
using System.Xml.Linq;

namespace ShaderEditorApp.Projects
{
	// Types of file that can be included in the project.
	public enum ProjectItemType
	{
		Other,
		Script,
		Shader,
		Scene,
	}

	// A file entry in a project.
	public class ProjectItem
	{
		internal ProjectItem(string path, ProjectFolder parent)
		{
			this.path = path;
			this.parent = parent;
		}

		public string Name { get { return Path.GetFileName(path); } }
		public string AbsolutePath { get { return path; } }
		public string Extension { get { return Path.GetExtension(path); } }

		public bool RunAtStartup { get; set; }
		public bool IsDefault { get; set; }

		// Get the type of file this item represents.
		public ProjectItemType Type
		{
			get
			{
				switch (Extension)
				{
					case ".py":
						return ProjectItemType.Script;
					case ".hlsl":
					case ".fx":
						return ProjectItemType.Shader;
					case ".srpscene":
						return ProjectItemType.Scene;
					default:
						return ProjectItemType.Other;
				}
			}
		}

		// Create a new instance based on data from an XML node.
		public static ProjectItem LoadFromElement(XElement item, ProjectFolder parent)
		{
			var result = new ProjectItem(Path.GetFullPath(Path.Combine(parent.Project.BasePath, item.Attribute("filename").Value)), parent);
			SerialisationUtils.ParseAttribute(item, "runAtStartup", str => result.RunAtStartup = bool.Parse(str));
			return result;
		}

		// Creates an XElement object corresponding to a project item, for saving.
		public XElement Save()
		{
			var element = new XElement("include");
			element.SetAttributeValue("filename", GetRelativePath(parent.Project.BasePath));
			element.SetAttributeValue("runAtStartup", RunAtStartup);
			return element;
		}

		// Remove this item from the project.
		public void RemoveFromProject()
		{
			if (parent == null)
				throw new InvalidOperationException("Cannot remove the root node.");

			parent.RemoveItem(this);
		}

		// Get the path to the underlying file relative to the given base.
		public string GetRelativePath(string basePath)
		{
			// .NET has no relative path function, so we have to use the Uri class.
			var baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
			var itemUri = new Uri(AbsolutePath);

			var relativeUri = baseUri.MakeRelativeUri(itemUri);

			if (!relativeUri.IsAbsoluteUri)
			{
				// Relative path found, so return it.
				return Uri.UnescapeDataString(relativeUri.ToString());
			}
			else
			{
				// No relative path, so just use the absolute path.
				return AbsolutePath;
			}
		}

		private string path;
		private ProjectFolder parent;
	}
}
