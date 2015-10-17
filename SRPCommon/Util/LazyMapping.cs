using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.Util
{
	public class LazyMapping<TSource, TDest> where TDest : IDisposable
	{
		private Dictionary<TSource, TDest> _dict = new Dictionary<TSource, TDest>();
		private Func<TSource, TDest> _create;

		public IEnumerable<TDest> Result { get { return _dict.Values; } }

		public LazyMapping(Func<TSource, TDest> create)
		{
			_create = create;
		}

		public void Update(IEnumerable<TSource> sources)
		{
			// Create new dests for sources not in the dictionary.
			foreach (var source in sources)
			{
				if (!_dict.ContainsKey(source))
				{
					_dict.Add(source, _create(source));
				}
			}

			// Remove dests that no longer have a source.
			var toRemove = _dict.Where(kvp => !sources.Contains(kvp.Key)).ToList();
			foreach (var kvp in toRemove)
			{
				kvp.Value.Dispose();
				_dict.Remove(kvp.Key);
			}
		}
	}
}
