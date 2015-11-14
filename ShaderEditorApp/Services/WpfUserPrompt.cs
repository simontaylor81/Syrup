using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ShaderEditorApp.Interfaces;
using SRPCommon.Util;

namespace ShaderEditorApp.Services
{
	// Class for showing user prompts using MessageBox.Show.
	class WpfUserPrompt : IUserPrompt
	{
		// Show Yes-No-Cancel message box.
		public Task<UserPromptResult> ShowYesNoCancel(string message)
		{
			var result = MessageBox.Show(message, GlobalConfig.AppName, MessageBoxButton.YesNoCancel);

			// WPF message boxes are blocking, so no need for any async shenanigans.
			return Task.FromResult(_mbResultToUserPromptResult[result]);
		}

		// Show Yes-No message box.
		public Task<UserPromptResult> ShowYesNo(string message)
		{
			var result = MessageBox.Show(message, GlobalConfig.AppName, MessageBoxButton.YesNo);

			// WPF message boxes are blocking, so no need for any async shenanigans.
			return Task.FromResult(_mbResultToUserPromptResult[result]);
		}

		private static Dictionary<MessageBoxResult, UserPromptResult> _mbResultToUserPromptResult = new Dictionary<MessageBoxResult, UserPromptResult>
		{
			{ MessageBoxResult.OK, UserPromptResult.Ok },
			{ MessageBoxResult.Cancel, UserPromptResult.Cancel },
			{ MessageBoxResult.Yes, UserPromptResult.Yes },
			{ MessageBoxResult.No, UserPromptResult.No },
		};
	}
}
