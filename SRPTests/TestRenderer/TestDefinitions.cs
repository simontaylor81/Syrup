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
using Xunit.Abstractions;

namespace SRPTests.TestRenderer
{
	// Code for loading test definitions from a json file.
	static class TestDefinitions
	{
		private static readonly string _testDefinitionFile = Path.Combine(GlobalConfig.BaseDir, @"SRPTests\TestScripts\tests.json");

		private static Lazy<SerializedTestDefinitions> _tests = new Lazy<SerializedTestDefinitions>(LoadDefinitions);

		public static IEnumerable<object[]> RenderTests => GetTests(_tests.Value.render);
		public static IEnumerable<object[]> ComputeTests => GetTests(_tests.Value.compute);

		private static SerializedTestDefinitions LoadDefinitions()
		{
			// Load and deserialise the json file.
			var json = File.ReadAllText(_testDefinitionFile);
			return JsonConvert.DeserializeObject<SerializedTestDefinitions>(json);
		}

		// Get actual test parameters from an array of definitions from the json.
		private static IEnumerable<object[]> GetTests(IEnumerable<SerializedTestDefinition> tests)
		{
			return GetLanguageTests(tests.Where(test => test.python), "Python", ".py")
				.Concat(GetLanguageTests(tests.Where(test => test.cs), "CS", ".cs"));
		}

		private static IEnumerable<object[]> GetLanguageTests(IEnumerable<SerializedTestDefinition> tests, string subdir, string extension)
		{
			// Script paths are relative to the json file.
			var baseDir = Path.Combine(Path.GetDirectoryName(_testDefinitionFile), subdir);

			return tests
				// Flatten all combinations of variables for each test definition.
				.SelectMany(test => GetVariableCombinations(test.vars), (test, vars) => new { test, vars })
				.Select(testAndVars =>
				{
					var test = testAndVars.test;
					var vars = testAndVars.vars;

					var name = test.name != null ? FormatName(test.name, vars) : test.script;
					var filename = test.script + extension;
					var definition = new TestDefinition(Path.Combine(baseDir, filename), vars);

						// Pass test name as first paramater so you can see what's what when running tests.
						return new object[] { name, definition };
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

// Field is never assigned to, and will always have its default value null
// These are assigned by deserialisation.
#pragma warning disable CS0649

		// Classes used for file deserialisation.

		private class SerializedTestDefinitions
		{
			public IEnumerable<SerializedTestDefinition> render;
			public IEnumerable<SerializedTestDefinition> compute;
		}

		private class SerializedTestDefinition
		{
			public string script;
			public bool python;
			public bool cs;
			public string name;
			public Dictionary<string, object> vars;
		}

#pragma warning restore CS0649
	}

	// Class containing data about a test script to give to the individual test functions.
	// Don't pass the Script object directly, as we need to be able to serialise it
	// for the tests to show up properly in the test runner.
	public class TestDefinition : IXunitSerializable
	{
		private string _filename;
		private IDictionary<string, object> _vars;

		public Script Script
		{
			get
			{
				var script = new Script(_filename);

				// Add vars as globals so they can be accessed from the script.
				foreach (var kvp in _vars.EmptyIfNull())
				{
					script.TestParams.Add(kvp.Key, kvp.Value);
				}

				return script;
			}
		}

		// Used for deserialization only.
		public TestDefinition() { }

		public TestDefinition(string filename, IDictionary<string, object> vars)
		{
			_filename = filename;
			_vars = vars;
		}

		public void Deserialize(IXunitSerializationInfo info)
		{
			_filename = info.GetValue<string>(nameof(_filename));
			_vars = JsonConvert.DeserializeObject<Dictionary<string, object>>(info.GetValue<string>(nameof(_vars)));
		}

		public void Serialize(IXunitSerializationInfo info)
		{
			info.AddValue(nameof(_filename), _filename);
			info.AddValue(nameof(_vars), JsonConvert.SerializeObject(_vars));
		}
	}
}
