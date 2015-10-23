using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Interfaces
{
	// Interface for the "workspace", basically the top-level view model of the app.
	public interface IWorkspace
	{
		/// <summary>
		/// Find a file with the given name in the currently loaded project.
		/// </summary>
		/// <param name="name">Filename (without path) of the file to find.</param>
		/// <returns>The absolute path of the file if found, null otherwise.</returns>
		string FindProjectFile(string name);

		// Given an absolute or project-relative path, get an absolute path.
		string GetAbsolutePath(string path);
	}
}
