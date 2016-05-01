using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Dirty hacky reflection-based wrapper around Roslyn CompletionService.
	// This is necessary because CompletionService is internal.
	// This is horrible and bound to break, but it beats re-implementing everything.
	class CompletionServiceWrapper
	{
		private readonly MethodInfo _getCompletionListAsync;
		private readonly MethodInfo _isCompletionTriggerCharacterAsync;
		private readonly MethodInfo _createInvokeCompletionTriggerInfo;
		private readonly MethodInfo _createTypeCharTriggerInfo;
		private readonly PropertyInfo _completionList_Items;
		private readonly PropertyInfo _completionItem_DisplayText;
		private readonly PropertyInfo _completionItem_FilterText;
		private readonly PropertyInfo _completionItem_FilterSpan;
		private readonly TaskReflectionHelper _completionListTaskHelper;
		private readonly TaskReflectionHelper<bool> _boolTaskHelper;

		public CompletionServiceWrapper()
		{
			// Load the 'Features' assembly which contains the completion service.
			var assembly = Assembly.Load("Microsoft.CodeAnalysis.Features");

			var completionServiceType = assembly.GetType("Microsoft.CodeAnalysis.Completion.CompletionService");
			_getCompletionListAsync = completionServiceType.GetMethod("GetCompletionListAsync");
			_isCompletionTriggerCharacterAsync = completionServiceType.GetMethod("IsCompletionTriggerCharacterAsync");

			var completionTriggerInfoType = assembly.GetType("Microsoft.CodeAnalysis.Completion.CompletionTriggerInfo");
			_createInvokeCompletionTriggerInfo = completionTriggerInfoType.GetMethod("CreateInvokeCompletionTriggerInfo");
			_createTypeCharTriggerInfo = completionTriggerInfoType.GetMethod("CreateTypeCharTriggerInfo");

			var completionListType = assembly.GetType("Microsoft.CodeAnalysis.Completion.CompletionList");
			_completionListTaskHelper = new TaskReflectionHelper(completionListType);

			_boolTaskHelper = new TaskReflectionHelper<bool>();

			_completionList_Items = completionListType.GetProperty("Items");

			var completionItemType = assembly.GetType("Microsoft.CodeAnalysis.Completion.CompletionItem");
			_completionItem_DisplayText = completionItemType.GetProperty("DisplayText");
			_completionItem_FilterText = completionItemType.GetProperty("FilterText");
			_completionItem_FilterSpan = completionItemType.GetProperty("FilterSpan");
		}

		public async Task<IEnumerable<CompletionItem>> GetCompletionListAsync(
			Document document,
			int position,
			char? triggerChar,
			CancellationToken cancellationToken)
		{
			var triggerInfo = CreateTriggerInfo(triggerChar);
			var task = (Task)_getCompletionListAsync.InvokeStatic(
				document, position, triggerInfo, null, null, cancellationToken);

			await task.ConfigureAwait(false);

			// Extract result from generic task.
			var completionList = _completionListTaskHelper.GetResult(task);
			if (completionList != null)
			{
				var items = (IEnumerable<object>)_completionList_Items.GetValue(completionList);

				// Convert to something consumable without reflection.
				return items.Select(MakeCompletionItem);
			}
			return Enumerable.Empty<CompletionItem>();
		}

		public async Task<bool> IsCompletionTriggerCharacterAsync(
			Document document,
			int position,
			CancellationToken cancellationToken)
		{
			var task = (Task)_isCompletionTriggerCharacterAsync.InvokeStatic(document, position, null, cancellationToken);
			await task.ConfigureAwait(false);

			// Extract result from generic task.
			return _boolTaskHelper.GetResult(task);
		}

		private object CreateTriggerInfo(char? triggerChar)
		{
			if (triggerChar.HasValue)
			{
				_createTypeCharTriggerInfo.InvokeStatic(triggerChar.Value);
			}
			return _createInvokeCompletionTriggerInfo.InvokeStatic();
		}

		private CompletionItem MakeCompletionItem(object completionItem)
		{
			return new CompletionItem()
			{
				DisplayText = (string)_completionItem_DisplayText.GetValue(completionItem),
				// InsertionText is specific to Symbol completions, so use FilterText instead.
				InsertionText = (string)_completionItem_FilterText.GetValue(completionItem),
				StartOffset = ((TextSpan)_completionItem_FilterSpan.GetValue(completionItem)).Start,
			};
		}
	}

	internal static class ReflectionExtensions
	{
		public static object InvokeStatic(this MethodInfo method, params object[] parameters)
		{
			return method.Invoke(null, parameters);
		}
	}
}
