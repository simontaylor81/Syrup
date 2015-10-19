using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SRPCommon.Util
{
	public static class DictionaryExtensions
	{
		// Merge two dictionaries. Throws if duplicate keys are found.
		public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> dict1, Dictionary<TKey, TValue> dict2)
		{
			var result = new Dictionary<TKey, TValue>(dict1);

			foreach (var kvp in dict2)
			{
				if (result.ContainsKey(kvp.Key))
				{
					throw new InvalidOperationException("Cannot merge dictionaries with duplicate keys");
				}

				result.Add(kvp.Key, kvp.Value);
			}

			return result;
		}

		// Get a value from the dictionary, or add one if not found.
		public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> create)
		{
			// Try to find the value in the dictionary.
			TValue result;
			if (!dict.TryGetValue(key, out result))
			{
				// Not found, so create using delegate and add.
				result = create();
				dict.Add(key, result);
			}

			return result;
		}
	}
}
