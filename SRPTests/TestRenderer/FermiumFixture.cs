using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SRPTests.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SRPTests.TestRenderer
{
	// Fixture class for handling interaction with Fermium,
	// including retrieving expected results and reporting results.
	public class FermiumFixture : IDisposable
	{
		private readonly string _fermiumBaseUrl;
		private readonly string _fermiumProjectUrl;
		private readonly HttpClient _httpClient;
		private bool _isBuildSuccess = true;
		private Dictionary<string, Task<byte[]>> _expectedResultTasks;

		// Disable everything if we don't have an URL.
		public bool IsEnabled { get { return !string.IsNullOrEmpty(_fermiumProjectUrl); } }

		// Only report the build when running in CI (otherwise just retrieve expected results).
		public bool ReportBuild { get { return IsEnabled && CIHelper.IsCI; } }

		public FermiumFixture()
		{
			_fermiumBaseUrl = "http://localhost:65221";
			//var server = Environment.GetEnvironmentVariable("SRP_FERMIUM_SERVER");
			if (!string.IsNullOrEmpty(_fermiumBaseUrl))
			{
				if (!_fermiumBaseUrl.EndsWith("/"))
				{
					_fermiumBaseUrl += "/";
				}

				// TODO: Configurable project name?
				_fermiumProjectUrl = _fermiumBaseUrl + "api/projects/syrup/";
			}

			if (IsEnabled)
			{
				_httpClient = new HttpClient();
				Initialise().Wait();
			}
		}

		public void Dispose()
		{
			// Test run is over, so set success/failure.
			if (ReportBuild)
			{
				SetBuildStatus().Wait();
			}
		}

		// Get the expected result of a test.
		public Task<byte[]> GetExpectedResult(string testcaseName)
		{
			return _expectedResultTasks[testcaseName];
		}

		private async Task Initialise()
		{
			// Get the list of test cases for this project so we can kick off the expected result requests.
			var response = await _httpClient.GetStringAsync(_fermiumProjectUrl + "testcases")
				.ConfigureAwait(false);
			var testcases = JArray.Parse(response);

			// Initiate expected result requests (so we don't have to stall when they're needed).
			_expectedResultTasks = testcases
				.Select(tc => Tuple.Create(tc.Value<string>("name"), GetExpectedResult_Impl(tc)))
				.ToDictionary(tup => tup.Item1, tup => tup.Item2);

			if (ReportBuild)
			{
				// Add the new build to Fermium.
				await PostToFermium("builds", new
				{
					buildNumber = CIHelper.BuildNumber,
					version = CIHelper.Version,
					commit = CIHelper.Commit,
				});
			}
		}

		private async Task<byte[]> GetExpectedResult_Impl(dynamic testcaseSummary)
		{
			// Follow link in the summary to retrieve the full testcase object.
			var json = await _httpClient.GetStringAsync(_fermiumBaseUrl + (string)testcaseSummary._links.self.href)
				.ConfigureAwait(false);
			dynamic testcaseFull = JObject.Parse(json);

			// Check we have an expected result.
			var expectedResultBase64 = (string)testcaseFull.expectedResult;
			Assert.False(
				string.IsNullOrEmpty(expectedResultBase64),
				string.Format("Test case '{0}' does not have an expected result.", testcaseFull.name));

			// Convert to byte array.
			return Convert.FromBase64String(expectedResultBase64);
		}

		private Task SetBuildStatus()
		{
			return PostToFermium(
				string.Format("builds/{0}/status?success={1}", CIHelper.BuildNumber, _isBuildSuccess),
				null);
		}

		public async Task TestComplete(string test, bool isSuccess, byte[] image)
		{
			// Build failed if any tests failed.
			_isBuildSuccess &= isSuccess;

			if (ReportBuild)
			{
				await PostToFermium(
					string.Format("builds/{0}/testruns/{1}", CIHelper.BuildNumber, test),
					new { isSuccess = isSuccess, result = image})
					.ConfigureAwait(false);
			}
		}

		// Send a POST to Fermium.
		private async Task PostToFermium(string path, object body)
		{
			Assert.True(IsEnabled);

			HttpContent content = null;
			if (body != null)
			{
				content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
			}

			var response = await _httpClient.PostAsync(_fermiumProjectUrl + path, content)
				.ConfigureAwait(false);
			response.EnsureSuccessStatusCode();
		}
	}
}
