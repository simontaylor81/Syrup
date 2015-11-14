using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.Interfaces
{
	public enum UserPromptResult
	{
		Ok,
		Cancel,
		Yes,
		No,
		YesToAll,
		NoToAll,
	}

	// Interface for showing prompts (e.g. MessageBox) to the user.
	public interface IUserPrompt
	{
		// Show a Yes-No-Cancel prompt.
		Task<UserPromptResult> ShowYesNoCancel(string message);
		Task<UserPromptResult> ShowYesNo(string message);
	}
}
