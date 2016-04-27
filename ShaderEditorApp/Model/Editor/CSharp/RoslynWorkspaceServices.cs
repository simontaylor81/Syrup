using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Text;
using SRPCommon.Interfaces;
using SRPCommon.Scripting;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Wrapper for all sorts of Roslyn stuff.
	// Handles multiple open documents, falling back to files on disk.
	public class RoslynWorkspaceServices
	{
		private readonly CSharpCompilationOptions _compilationOptions;
		private readonly CSharpParseOptions _parseOptions;
		private readonly IEnumerable<PortableExecutableReference> _metadataReferences;
		private readonly MefHostServices _host;
		private readonly OpenDocumentReferenceResolver _referenceResolver;

		public RoslynWorkspaceServices(IWorkspace workspace)
		{
			// Load "Features" assemblies to get things like auto-complete.
			var additionalAssemblies = new[]
			{
				Assembly.Load("Microsoft.CodeAnalysis.Features"),
				Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features"),
			};

			_host = MefHostServices.Create(MefHostServices.DefaultAssemblies.Concat(additionalAssemblies));

			// TODO: Share this list with the scripting system.
			var usings = new[]
			{
				"System",
				"System.Linq",
				"System.Collections.Generic",
				"System.Numerics",
				"SRPScripting",
			};

			_referenceResolver = new OpenDocumentReferenceResolver(workspace);
			_compilationOptions = new CSharpCompilationOptions(
				OutputKind.DynamicallyLinkedLibrary,
				usings: usings,
				sourceReferenceResolver: _referenceResolver);

			_parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script);

			// TODO: Share this list with the scripting system.
			var imports = new[]
			{
				typeof(object).Assembly,							// mscorlib.dll
				typeof(Uri).Assembly,								// System.
				typeof(Enumerable).Assembly,						// System.Core
				typeof(SRPScripting.IRenderInterface).Assembly,
				typeof(CSharpGlobals).Assembly,
				typeof(System.Numerics.Vector3).Assembly,
			};
			// TODO: This stuff is fairly expensive and should be done only once!
			_metadataReferences = imports.Select(a => MetadataReference.CreateFromFile(a.Location));
		}

		public IDocumentServices OpenDocument(SourceTextContainer sourceTextContainer, string path)
		{
			// Each document gets its own Roslyn workspace.
			// Roslyn doesn't support multiple script files in the same project (it throws lots of exceptions).
			// Don't know if multiple workspaces or multiple projects in a workspace is more efficient,
			// but this way is simpler.
			var roslynWorkspace = new RoslynScriptWorkspace(_host);

			var documentName = Path.GetFileName(path);
			var projectName = documentName + "_project";
			var projectId = ProjectId.CreateNewId(projectName);
			var documentId = DocumentId.CreateNewId(projectId, documentName);

			// Create Roslyn document for this file.
			var documentInfo = DocumentInfo.Create(
				documentId,
				documentName,
				sourceCodeKind: SourceCodeKind.Script,
				loader: TextLoader.From(TextAndVersion.Create(sourceTextContainer.CurrentText, VersionStamp.Create())),
				filePath: path);

			// Add a project to the workspace containing just that file.
			var solution = roslynWorkspace.CurrentSolution.AddProject(ProjectInfo.Create(
				projectId,
				VersionStamp.Default,
				projectName,
				projectName,
				LanguageNames.CSharp,
				compilationOptions: _compilationOptions,
				parseOptions: _parseOptions,
				documents: new[] { documentInfo },
				metadataReferences: _metadataReferences,
				isSubmission: true,
				hostObjectType: typeof(CSharpGlobals)
				));

			roslynWorkspace.SetSolution(solution);
			roslynWorkspace.OpenDocument(documentId, sourceTextContainer);

			// Add this document to the reference resolver so scripts that reference it
			// use the in-memory version rather than the version on disk.
			_referenceResolver.OpenDocument(path, sourceTextContainer);

			return new RoslynDocumentServices(roslynWorkspace, documentId, () =>
			{
				// Roslyn workspace is just discarded so no need to close or remove anything.

				// Remove from reference resolver so we return to looking up references on disk.
				_referenceResolver.CloseDocument(path);
			});
		}
	}
}
