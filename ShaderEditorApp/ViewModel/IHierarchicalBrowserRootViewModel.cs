using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel
{
	/// <summary>
	/// Interface for view model objects representing the whole tree in the hierarchical browser.
	/// </summary>
	public interface IHierarchicalBrowserRootViewModel
	{
		/// <summary>
		/// The currently active node in the tree (i.e. the one the user has clicked on).
		/// </summary>
		IHierarchicalBrowserNodeViewModel ActiveItem { get; set; }
	}
}
