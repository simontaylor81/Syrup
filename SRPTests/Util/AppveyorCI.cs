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

		public bool IsCI { get { return true; } }
		public bool IsAppVeyor { get { return true; } }
		public bool IsDummy { get { return false; } }

		public string BuildNumber
		{
			get
			{
				return Environment.GetEnvironmentVariable("APPVEYOR_BUILD_NUMBER");
			}
		}

		public string Version
		{
			get
			{
				return Environment.GetEnvironmentVariable("APPVEYOR_BUILD_VERSION");
			}
		}

		public string Commit
		{
			get
			{
				return Environment.GetEnvironmentVariable("APPVEYOR_REPO_COMMIT");
			}
		}

		public async Task PublishArtefactAsync(string path)
		{
			Console.WriteLine("Publishing artefact {0}", path);

			try
			{
				var appveyorApiUrl = Environment.GetEnvironmentVariable("APPVEYOR_API_URL");
				var jsonRequest = JsonConvert.SerializeObject(new
				{
					path = Path.GetFullPath(path),
					fileName = Path.GetFileName(path),
					name = (string)null,
				});

				Console.WriteLine("APPVEYOR_API_URL = {0}", appveyorApiUrl);
				Console.WriteLine("jsonRequest = {0}", jsonRequest);

				// PUT data to api URL to get where to upload the file to.
				var httpClient = new HttpClient();
				var response = await httpClient.PostAsync(
					appveyorApiUrl + "api/artifacts",
					new StringContent(jsonRequest, Encoding.UTF8, "application/json")
					).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				var uploadUrl = JsonConvert.DeserializeObject<string>(responseString);

				Console.WriteLine("responseString = {0}", responseString);
				Console.WriteLine("uploadUrl = {0}", uploadUrl);

				// Upload the file to the returned URL.
				using (var wc = new WebClient())
				{
					await wc.UploadFileTaskAsync(new Uri(uploadUrl), path).ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error uploading artefact.");
				Console.WriteLine(ex.Message);
			}
		}
	}
}
