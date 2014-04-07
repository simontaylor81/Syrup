using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SRPCommon.UserProperties;

namespace ShaderEditorApp.ViewModel
{
	/// <summary>
	/// Interface for view model objects representing a node in the hierarchical tree browser.
	/// </summary>
	public interface IHierarchicalBrowserNodeViewModel
	{
		/// <summary>
		/// Name of the node to show to the user.
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Commnads that can be executed on this node (used for drop-down menu).
		/// </summary>
		IEnumerable<ICommand> Commands { get; }

		/// <summary>
		/// Set of properties that this node exposes.
		/// </summary>
		IEnumerable<IUserProperty> UserProperties { get; }

		/// <summary>
		/// List of child nodes. null if this is a leaf node.
		/// </summary>
		IEnumerable<IHierarchicalBrowserNodeViewModel> Children { get; }

		/// <summary>
		/// 'Default' command -- i.e. the one to execute when double clicking on the item.
		/// </summary>
		ICommand DefaultCmd { get; }

		/// <summary>
		/// Is this the 'default' node of its type? I.e. should it be bold in the UI.
		/// </summary>
		bool IsDefault { get; }
	}
}
