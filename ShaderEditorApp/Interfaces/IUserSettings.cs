
namespace ShaderEditorApp.Interfaces
{
	public interface IUserSettings
	{
		IRecentFileList RecentFiles { get; }
		IRecentFileList RecentProjects { get; }

		void Save();
	}
}
