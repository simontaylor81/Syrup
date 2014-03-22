using System;
using System.Collections.Generic;
using System.Linq;
using ShaderEditorApp.MVVMUtil;
using ShaderEditorApp.ViewModel;

namespace ShaderEditorApp.Projects
{
	public abstract class ProjectViewModelBase : ViewModelBase
	{
		public IEnumerable<NamedCommand> Commands { get; protected set; }
		public abstract IEnumerable<PropertyViewModel> ItemProperties { get; }
	}
}
