using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SRPCommon.Interfaces;

namespace SRPCommon.Scripting
{
	// Roslyn reference resolver that finds files in a workspace.
	// Falls back to the file system if not found.
	public class WorkspaceReferenceResolver : SourceReferenceResolver
	{
		private readonly IWorkspace _workspace;
		private readonly SourceReferenceResolver _fallback = SourceFileResolver.Default;

		public WorkspaceReferenceResolver(IWorkspace workspace)
		{
			_workspace = workspace;
		}

		public override bool Equals(object other)
		{
			var otherResolver = other as WorkspaceReferenceResolver;
			return otherResolver != null && _workspace == otherResolver._workspace;
		}

		public override int GetHashCode() => _workspace.GetHashCode();

		// Let the default file system handler normalise paths.
		// I don't think this is every actually called, anyway.
		public override string NormalizePath(string path, string baseFilePath)
			=> _fallback.NormalizePath(path, baseFilePath);

		// The default resolver is perfectly capable of opening a stream for a path.
		public override Stream OpenRead(string resolvedPath) => _fallback.OpenRead(resolvedPath);

		public override string ResolveReference(string path, string baseFilePath)
		{
			// Is the path unqualified?
			if (string.Equals(path, Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
			{
				// Check the workspace for the file.
				return _workspace.FindProjectFile(path);
			}

			// Fallback to default handler to allow file-relative and absolute paths.
			return _fallback.ResolveReference(path, baseFilePath);
		}
	}
}
