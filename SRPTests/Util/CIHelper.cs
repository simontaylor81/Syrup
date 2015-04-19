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
		// Are we running under Appveyor?
		public static bool IsAppveyor { get { return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPVEYOR")); } }

		// Are we running under a CI server?
		public static bool IsCI { get { return IsAppveyor; } }

		// Various properties of the build currently being built.
		public static string BuildNumber
		{
			get
			{
				// TODO
				return "67";
			}
		}

		public static string Version
		{
			get
			{
				// TODO
				return "1.0.1";
			}
		}

		public static string Commit
		{
			get
			{
				// TODO
				return "abcdef";
			}
		}

		// Publish an artefact to the CI server.
		public static Task PublishArtefact(string path)
		{
			Console.WriteLine("Publishing artefact {0}", path);
			if (IsAppveyor)
			{
				// Running under Appveyor, so publish artefact using REST api.
				return PublishArtefact_Appveyor(path);
			}
			else
			{
				// Running locally, so just ShellExecute the file.
				Process.Start(path);
				return Task.Delay(0); // this is fast tracked internally
            }
		}

		private static async Task PublishArtefact_Appveyor(string path)
		{
			try
			{
				var appveyorApiUrl = Environment.GetEnvironmentVariable("APPVEYOR_API_URL");
				var jsonRequest = JsonConvert.SerializeObject(new
				{
					path = Path.GetFullPath(path),
					fileName = Path.GetFileName(path),
					name = (string)null,
					//type = "html"
				});

				Console.WriteLine("APPVEYOR_API_URL = {0}", appveyorApiUrl);
				Console.WriteLine("jsonRequest = {0}", jsonRequest);

				// PUT data to api URL to get where to upload the file to.
				var httpClient = new HttpClient();
				var response = await httpClient.PostAsync(appveyorApiUrl + "api/artifacts", new StringContent(jsonRequest, Encoding.UTF8, "application/json")).ConfigureAwait(false);
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
