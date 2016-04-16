using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShaderEditorApp.Model.Editor
{
	public interface ICodeTipProvider
	{
		Task<string> GetCodeTipAsync(int offset, CancellationToken cancellationToken);
	}
}
