using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ShaderEditorApp.MVVMUtil;
using System.Diagnostics;
using SlimDX;
using SRPCommon.UserProperties;

namespace ShaderEditorApp.ViewModel
{
	// Represents a property that can be displayed and edited in the properties window.
	// Properties have a type, a name and a value.
	public abstract class PropertyViewModel : ViewModelBase
	{
		public PropertyViewModel(IUserProperty property)
		{
			DisplayName = property.Name;
			IsReadOnly = property.IsReadOnly;
		}

		public bool IsReadOnly { get; private set; }
	}
}
