using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	class DocumentationHelper
	{
		private readonly MethodInfo _fromXmlFragment;
		private readonly PropertyInfo _exampleText;
		private readonly PropertyInfo _summaryText;
		private readonly PropertyInfo _returnsText;
		private readonly PropertyInfo _remarksText;

		public DocumentationHelper()
		{
			// We have to use reflection again because the documentation classes are internal.
			var assembly = Assembly.Load("Microsoft.CodeAnalysis.Workspaces");

			var documentationCommentType = assembly.GetType("Microsoft.CodeAnalysis.Shared.Utilities.DocumentationComment");
			_fromXmlFragment = documentationCommentType.GetMethod("FromXmlFragment");

			_exampleText = documentationCommentType.GetProperty("ExampleText");
			_summaryText = documentationCommentType.GetProperty("SummaryText");
			_returnsText = documentationCommentType.GetProperty("ReturnsText");
			_remarksText = documentationCommentType.GetProperty("RemarksText");
		}

		public string GetDocumentationString(ISymbol symbol)
		{
			var xml = symbol.GetDocumentationCommentXml();
			var documentation = _fromXmlFragment.InvokeStatic(xml);
			return GetSummaryText(documentation);
		}

		private string GetExampleText(object documentationComment) => (string)_exampleText.GetValue(documentationComment);
		private string GetSummaryText(object documentationComment) => (string)_summaryText.GetValue(documentationComment);
		private string GetReturnsText(object documentationComment) => (string)_returnsText.GetValue(documentationComment);
		private string GetRemarksText(object documentationComment) => (string)_remarksText.GetValue(documentationComment);
	}
}
