using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using SRPCommon.Interfaces;
using SRPCommon.Scripting;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Reference resolver that can handle open documents.
	class OpenDocumentReferenceResolver : WorkspaceReferenceResolver
	{
		private Dictionary<string, SourceTextContainer> _openFiles = new Dictionary<string, SourceTextContainer>();

		public OpenDocumentReferenceResolver(IWorkspace workspace)
			: base(workspace)
		{ }

		public void OpenDocument(string path, SourceTextContainer text)
		{
			Trace.Assert(!_openFiles.ContainsKey(path));
			_openFiles.Add(path, text);
		}

		public void CloseDocument(string path)
		{
			Trace.Assert(_openFiles.ContainsKey(path));
			_openFiles.Remove(path);
		}

		public override SourceText ReadText(string resolvedPath)
		{
			SourceTextContainer textContainer;
			if (_openFiles.TryGetValue(resolvedPath, out textContainer))
			{
				return textContainer.CurrentText;
			}
			return base.ReadText(resolvedPath);
		}
	}
}
