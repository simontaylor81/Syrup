using System;
using System.Collections.Generic;
using System.Linq;
using ShaderEditorApp.MVVMUtil;
using ShaderEditorApp.ViewModel;
using SRPCommon.UserProperties;

namespace ShaderEditorApp.Projects
{
	public abstract class ProjectViewModelBase : ViewModelBase
	{
		public IEnumerable<NamedCommand> Commands { get; protected set; }
		internal IEnumerable<IUserProperty> ItemProperties { get; set; }
	}
}
