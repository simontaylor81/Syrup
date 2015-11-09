using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.Model
{
	// Class encapsulating all user settings, whether they be configuration options
	// or saved state like recently opened files.
	public class UserSettings
	{
		public RecentFileList RecentProjects { get; } = new RecentFileList(10);
	}
}
