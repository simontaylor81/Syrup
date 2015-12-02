using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SRPTests.Util
{
	// AppVeyor CI provider
	internal class AppveyorCI : ICIProvider
	{
		// Create an instance if we're running in AppVeyor
		public static ICIProvider ConditionalCreate()
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPVEYOR")))
			{
				return new AppveyorCI();
			}
			return null;
		}

		public bool IsCI => true;
		public bool IsAppVeyor => true;
		public bool IsDummy => false;

		public string BuildNumber => Environment.GetEnvironmentVariable("APPVEYOR_BUILD_NUMBER");
		public string Version => Environment.GetEnvironmentVariable("APPVEYOR_BUILD_VERSION");
		public string Commit => Environment.GetEnvironmentVariable("APPVEYOR_REPO_COMMIT");

		private string _appveyorApiUrl = Environment.GetEnvironmentVariable("APPVEYOR_API_URL");

		private HttpClient _httpClient = new HttpClient();

		public async Task PublishArtefactAsync(string path)
		{
			Console.WriteLine("Publishing artefact {0}", path);

			try
			{
				var jsonRequest = JsonConvert.SerializeObject(new
				{
					path = Path.GetFullPath(path),
					fileName = Path.GetFileName(path),
					name = (string)null,
				});

				Console.WriteLine("APPVEYOR_API_URL = {0}", _appveyorApiUrl);
				Console.WriteLine("jsonRequest = {0}", jsonRequest);

				// PUT data to api URL to get where to upload the file to.
				var response = await _httpClient.PostAsync(
					_appveyorApiUrl + "api/artifacts",
					new StringContent(jsonRequest, Encoding.UTF8, "application/json")
					).ConfigureAwait(false);

				if (!response.IsSuccessStatusCode)
				{
					await LogFailedHttpRequest(response, "getting artefact upload URL");
					return;
				}

				var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				var uploadUrl = JsonConvert.DeserializeObject<string>(responseString);

				Console.WriteLine("responseString = {0}", responseString);
				Console.WriteLine("uploadUrl = {0}", uploadUrl);

				// Upload the file to the returned URL.
				await UploadFile(uploadUrl, path);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error uploading artefact.");
				Console.WriteLine(ex.Message);
			}
		}

		// Upload an artefact file to the given URL.
		private Task UploadFile(string url, string filename)
		{
			// Method is different if uploading to Google storage.
			if (url.ToLowerInvariant().Contains("storage.googleapis.com"))
			{
				return UploadFileGoogleStorage(url, filename);
			}
			else
			{
				return UploadFileWebClient(url, filename);
			}
		}

		// Upload file using WebClient.
		private async Task UploadFileWebClient(string url, string path)
		{
			try
			{
				using (var wc = new WebClient())
				{
					await wc.UploadFileTaskAsync(new Uri(url), path).ConfigureAwait(false);
				}
			}
			catch (WebException ex) when (ex.Response is HttpWebResponse)
			{
				var response = (HttpWebResponse)ex.Response;

				Console.WriteLine("Error uploading artefact.");
				Console.WriteLine($"Status code: {response.StatusCode}");

				using (var reader = new StreamReader(response.GetResponseStream()))
				{
					Console.WriteLine("Reponse:");
					Console.WriteLine(reader.ReadToEnd());
				}
			}
		}

		// Upload a file to Google storage.
		private async Task UploadFileGoogleStorage(string url, string path)
		{
			using (var fileStream = File.OpenRead(path))
			{
				// PUT file contents to remote URL.
				var content = new StreamContent(fileStream);
				var response = await _httpClient.PutAsync(url, content);

				if (!response.IsSuccessStatusCode)
				{
					await LogFailedHttpRequest(response, "uploading artefact to Google storage");

					// Fail silently -- don't want to fail the build for failed artefact upload.
					return;
				}

				// 'Finalise' the upload by PUTing to the AppVeyor API again.
				// PUT data to api URL to get where to upload the file to.
				response = await _httpClient.PutAsJsonAsync(
					_appveyorApiUrl + "api/artifacts",
					new { fileName = Path.GetFileName(path), size = fileStream.Length }
					).ConfigureAwait(false);

				if (!response.IsSuccessStatusCode)
				{
					await LogFailedHttpRequest(response, "getting artefact upload URL");
					return;
				}
			}
		}

		// Helper to log info about a failed rest call.
		private async Task LogFailedHttpRequest(HttpResponseMessage response, string desc)
		{
			Console.WriteLine($"Error {desc}.");
			Console.WriteLine($"Status code: {response.StatusCode}");
			Console.WriteLine("Response:");
			Console.WriteLine(await response.Content.ReadAsStringAsync());
		}
	}
}
