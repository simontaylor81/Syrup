using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Class for providing signature help.
	// I.e. parameter info for function calls.
	// All this code owes a great dept to Omnisharp (https://github.com/OmniSharp/omnisharp-roslyn), and
	// some of it is a direct copy-and-paste (MIT license).
	internal static class SignatureHelpService
	{
		public static async Task<SignatureHelp> GetSignatureHelp(Document document, int position, CancellationToken cancellationToken)
		{
			var invocation = await FindInvocation(document, position, cancellationToken);
			if (invocation == null)
			{
				// Not in an invocation.
				return null;
			}

			var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

			var paramTypes = invocation.Item2.Arguments.Select(arg => semanticModel.GetTypeInfo(arg.Expression, cancellationToken))
				.ToList();

			var overloads = GetOverloads(semanticModel, invocation.Item1);

			var activeParameter = GetActiveParameter(invocation.Item2, position);

			return new SignatureHelp
			{
				ActiveParameter = activeParameter,
				// TODO: Best overload.
				Overloads = overloads.Select(overload =>
				{
					var nameSymbol = overload.MethodKind == MethodKind.Constructor ? (ISymbol)overload.ContainingType : overload;
					return new SignatureHelpOverload
					{
						Name = nameSymbol.Name,
						Label = nameSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
						Documentation = overload.GetDocumentationCommentXml(cancellationToken: cancellationToken),
						Parameters = GetParameters(overload)
							.Select(param => new SignatureHelpParameter
							{
								Name = param.Name,
								Label = param.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
								Documentation = param.GetDocumentationCommentXml(cancellationToken: cancellationToken),
							}),
					};
				})
			};
		}

		// Find the syntax node of the invocation that the caret is in.
		private static async Task<Tuple<SyntaxNode, ArgumentListSyntax>> FindInvocation(
			Document document, int position, CancellationToken cancellationToken)
		{
			// Find the right position in the syntax tree.
			var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
			var node = syntaxRoot.FindToken(position).Parent;

			// Find method or object creation invocation.
			while (node != null)
			{
				var invocation = node as InvocationExpressionSyntax;
				if (invocation != null && invocation.ArgumentList.Span.Contains(position))
				{
					return new Tuple<SyntaxNode, ArgumentListSyntax>(invocation, invocation.ArgumentList);
				}

				var objectCreation = node as ObjectCreationExpressionSyntax;
				if (objectCreation != null && objectCreation.ArgumentList.Span.Contains(position))
				{
					return new Tuple<SyntaxNode, ArgumentListSyntax>(objectCreation, objectCreation.ArgumentList);
				}

				node = node.Parent;
			}

			return null;
		}

		// Work out which parameter the caret is in.
		private static int GetActiveParameter(ArgumentListSyntax argumentList, int position)
		{
			var result = 0;
			foreach (var separator in argumentList.Arguments.GetSeparators())
			{
				if (separator.Span.Start > position)
				{
					break;
				}
				result++;
			}

			return result;
		}

		// Direct copy from Omnisharp.
		private static IEnumerable<IMethodSymbol> GetOverloads(SemanticModel semanticModel, SyntaxNode node)
		{
			ISymbol symbol = null;
			var symbolInfo = semanticModel.GetSymbolInfo(node);
			if (symbolInfo.Symbol != null)
			{
				symbol = symbolInfo.Symbol;
			}
			else if (!symbolInfo.CandidateSymbols.IsEmpty)
			{
				symbol = symbolInfo.CandidateSymbols.First();
			}

			if (symbol == null || symbol.ContainingType == null)
			{
				return new IMethodSymbol[] { };
			}

			return symbol.ContainingType.GetMembers(symbol.Name).OfType<IMethodSymbol>();
		}

		// Direct copy from Omnisharp.
		private static IEnumerable<IParameterSymbol> GetParameters(IMethodSymbol methodSymbol)
		{
			if (!methodSymbol.IsExtensionMethod)
			{
				return methodSymbol.Parameters;
			}
			else
			{
				return methodSymbol.Parameters.RemoveAt(0);
			}
		}
	}
}
