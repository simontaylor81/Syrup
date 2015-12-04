using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SRPCommon.Scripting;
using SRPCommon.Util;
using Xunit;

namespace SRPTests.TestRenderer
{
	// Code for loading test definitions from a json file.
	static class TestDefinitions
	{
		public static IEnumerable<object[]> Load(string filename)
		{
			// Load and deserialise the json file.
			var json = File.ReadAllText(filename);
			var tests = JsonConvert.DeserializeObject<IEnumerable<TestDefinition>>(json);

			// Script paths are relative to the json file.
			var baseDir = Path.GetDirectoryName(filename);

			return tests
				// Flatten all combinations of variables for each test definition.
				.SelectMany(test => GetVariableCombinations(test.vars), (test, vars) => new { test, vars })
				.Select(testAndVars =>
				{
					var test = testAndVars.test;
					var vars = testAndVars.vars;

					var script = new Script(Path.Combine(baseDir, test.script));

					// Add vars as globals so they can be accessed from the script.
					foreach (var kvp in vars.EmptyIfNull())
					{
						script.GlobalVariables.Add(kvp.Key, kvp.Value);
					}

					var name = test.name != null ? FormatName(test.name, vars) : Path.GetFileNameWithoutExtension(test.script);

					// Pass test name as first paramater so you can see what's what when running tests.
					return new object[] { name, script };
				});
		}

		// Replace '{var}' in format string with value of 'var' in values dictionary.
		private static string FormatName(string format, IDictionary<string, object> values)
		{
			// This isn't in the slightest bit robust, so may need changing.
			var regex = new Regex(@"{([^}]+)}");
			return regex.Replace(format, m => values[m.Groups[1].Value].ToString());
		}

		private static IEnumerable<IDictionary<string, object>> GetVariableCombinations(Dictionary<string, object> vars)
		{
			// Consolidate variables so everything's an array of key-value pairs.
			var allVars = vars.EmptyIfNull()
				.Select(v =>
				{
					var array = v.Value as JArray;
					if (array != null)
					{
						return array
							.Cast<JValue>()
							.Select(val => new KeyValuePair<string, object>(v.Key, val.Value));
					}
					return new[] { new KeyValuePair<string, object>(v.Key, v.Value) };
				});

			// Return cartesian product of all possible values, converted back to dictionary.
			return CartesianProduct(allVars)
				.Select(varSet => varSet.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
		}

		// Compute cartesian product of list-of-lists.
		private static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> inputs) =>
			inputs.Aggregate(
				EnumerableEx.Return(Enumerable.Empty<T>()),
				(acc, input) =>
					from prevProductItem in acc
					from item in input
					select prevProductItem.Concat(EnumerableEx.Return(item)));

		// Class used for file deserialisation.
		private class TestDefinition
		{
// Field is never assigned to, and will always have its default value null
// These are assigned by deserialisation.
#pragma warning disable CS0649
			public string script;
			public string name;
			public string expectedResult;
			public Dictionary<string, object> vars;
#pragma warning restore CS0649
		}
	}
}
