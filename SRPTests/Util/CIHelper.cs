﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SRPTests.Util
{
	// Continuous integration helper functionality.
	// Currently supports Appveyor
	static class CIHelper
	{
		public static bool IsAppveyor { get { return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPVEYOR")); } }

		public static async Task PublishArtefact(string path)
		{
			if (IsAppveyor)
			{
				// Running under Appveyor, so publish artefact using REST api.
				await PublishArtefact_Appveyor(path);
			}
			else
			{
				// Running locally, so just ShellExecute the file.
				Process.Start(path);
            }
		}

		private static async Task PublishArtefact_Appveyor(string path)
		{
			var appveyorApiUrl = Environment.GetEnvironmentVariable("APPVEYOR_API_URL");
			var jsonRequest = JsonConvert.SerializeObject(new
			{
				path = Path.GetFullPath(path),
				fileName = Path.GetFileName(path),
				name = (string)null,
				type = "html"
			});

			// PUT data to api URL to get where to upload the file to.
			var httpClient = new HttpClient();
			var response = await httpClient.PutAsync(appveyorApiUrl + "api/artifacts", new StringContent(jsonRequest, Encoding.UTF8, "application /json"));
			response.EnsureSuccessStatusCode();

			var responseString = await response.Content.ReadAsStringAsync();
			var uploadUrl = JsonConvert.DeserializeObject<string>(responseString);

			// Upload the file to the returned URL.
			await new WebClient().UploadFileTaskAsync(new Uri(uploadUrl), path);
		}
	}
}
