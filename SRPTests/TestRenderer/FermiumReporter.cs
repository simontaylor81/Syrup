using Newtonsoft.Json;
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
	public class FermiumReporter : IDisposable
	{
		private readonly string _fermiumBaseUrl;
		private readonly string _fermiumProjectUrl;
		private readonly HttpClient _httpClient;
		private bool _isBuildSuccess = true;

		// Report builds to Fermium if we have a URL, and we're running in CI.
		// For now, only enable in the dummy CI provider as we don't have a publically-accessible Fermium instance.
		public bool IsEnabled { get { return CIHelper.IsCI && !string.IsNullOrEmpty(_fermiumProjectUrl) && CIHelper.IsDummy; } }

		public FermiumReporter()
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
			if (IsEnabled)
			{
				SetBuildStatus().Wait();
			}
		}

		private Task Initialise()
		{
			// Add the new build to Fermium.
			return PostToFermium("builds", new
			{
				buildNumber = CIHelper.BuildNumber,
				version = CIHelper.Version,
				commit = CIHelper.Commit,
			});
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

			if (IsEnabled)
			{
				await PostToFermium(
					string.Format("builds/{0}/testruns/{1}", CIHelper.BuildNumber, test),
					new { isSuccess = isSuccess, result = image})
					.ConfigureAwait(false);
			}
		}

		// Helper to send a POST to Fermium with optional json-encoded body object.
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
