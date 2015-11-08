using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SRPCommon.UserProperties;
using ReactiveUI;

namespace ShaderEditorApp.ViewModel
{
	// Represents a property that can be displayed and edited in the properties window.
	// Properties have a type, a name and a value.
	public abstract class PropertyViewModel : ReactiveObject
	{
		protected PropertyViewModel(IUserProperty property)
		{
			DisplayName = property.Name;
			IsReadOnly = property.IsReadOnly;
		}

		public string DisplayName { get; }
		public bool IsReadOnly { get; }
	}
}
