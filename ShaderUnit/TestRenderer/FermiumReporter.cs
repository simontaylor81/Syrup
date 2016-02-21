using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ShaderUnit.Util;

namespace ShaderUnit.TestRenderer
{
	// Fixture class for handling interaction with Fermium,
	// including retrieving expected results and reporting results.
	class FermiumReporter : ITestReporter
	{
		private readonly string _fermiumProjectUrl;
		private readonly HttpClient _httpClient;
		private bool _isBuildSuccess = true;

		// Report builds to Fermium if we have a URL, and we're running in CI.
		public static bool CanUse => CIHelper.IsCI && !string.IsNullOrEmpty(BaseUrl);

		private static string BaseUrl => Environment.GetEnvironmentVariable("SRP_FERMIUM_SERVER");

		public FermiumReporter()
		{
			Trace.Assert(CanUse);

			var baseUrl = BaseUrl;
			if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
			{
				baseUrl += "/";
			}

			// TODO: Configurable project name?
			_fermiumProjectUrl = baseUrl + "api/projects/syrup/";

			_httpClient = new HttpClient();
		}

		public Task InitialiseAsync()
		{
			// Add the new build to Fermium.
			return PostToFermium("builds", new
			{
				buildNumber = CIHelper.BuildNumber,
				version = CIHelper.Version,
				commit = CIHelper.Commit,
			});
		}

		public Task DisposeAsync()
		{
			// Test run is over, so set success/failure.
			return SetBuildStatus();
		}

		private Task SetBuildStatus()
		{
			return PostToFermium(
				string.Format("builds/{0}/status?success={1}", CIHelper.BuildNumber, _isBuildSuccess),
				null);
		}

		public async Task TestCompleteAsync(string test, bool isSuccess, Bitmap result)
		{
			// Build failed if any tests failed.
			_isBuildSuccess &= isSuccess;

			await PostToFermium(
				string.Format("builds/{0}/testruns/{1}", CIHelper.BuildNumber, test),
				new { isSuccess = isSuccess, result = BitmapToBytes(result) })
				.ConfigureAwait(false);
		}

		// Helper to send a POST to Fermium with optional json-encoded body object.
		private async Task PostToFermium(string path, object body)
		{
			HttpContent content = null;
			if (body != null)
			{
				content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
			}

			var response = await _httpClient.PostAsync(_fermiumProjectUrl + path, content)
				.ConfigureAwait(false);
			response.EnsureSuccessStatusCode();
		}

		// Convert an image to PNG-encoded byte array.
		private byte[] BitmapToBytes(Bitmap bitmap)
		{
			byte[] result = null;
			if (bitmap != null)
			{
				using (var stream = new MemoryStream())
				{
					bitmap.Save(stream, ImageFormat.Png);
					result = stream.ToArray();
				}
			}

			return result;
		}
	}
}
