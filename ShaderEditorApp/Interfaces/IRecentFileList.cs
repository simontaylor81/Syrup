using ReactiveUI;

namespace ShaderEditorApp.Interfaces
{
	public interface IRecentFileList
	{
		IReadOnlyReactiveList<string> Files { get; }
		int MaxSize { get; set; }

		void AddFile(string filename);
	}
}
