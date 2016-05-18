using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShaderEditorApp.Model.Editor.CSharp;
using SRPCommon.Interfaces;

namespace ShaderEditorApp.Model.Editor
{
	// Class for making appropriate IDocumentServices implementation for a particular language/filetype.
	public class DocumentServicesFactory
	{
		private readonly RoslynWorkspaceServices _csharpEditorServices;

		public DocumentServicesFactory(IWorkspace workspace)
		{
			_csharpEditorServices = new RoslynWorkspaceServices(workspace);
		}

		// TODO: Language-agnotic form of SourceTextContainer?
		public IDocumentServices OpenDocument(string path, Microsoft.CodeAnalysis.Text.SourceTextContainer sourceTextContainer)
		{
			switch (Path.GetExtension(path).ToLowerInvariant())
			{
				// Only C# has special behaviour currently.
				case ".csx":
				case ".cs":
					return _csharpEditorServices.OpenDocument(sourceTextContainer, path);

				default:
					return new NullDocumentServices();
			}
		}
	}
}
