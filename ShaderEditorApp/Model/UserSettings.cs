using Newtonsoft.Json;
using SRPCommon.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.Model
{
	// Class encapsulating all user settings, whether they be configuration options
	// or saved state like recently opened files.
	public class UserSettings
	{
		public RecentFileList RecentProjects { get; } = new RecentFileList(10);
		public RecentFileList RecentFiles { get; } = new RecentFileList(10);

		private string Filename => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			GlobalConfig.AppName,
			"settings.json");

		public UserSettings()
		{
			try
			{
				// Load the settings from disk.
				var json = File.ReadAllText(Filename);
				JsonConvert.PopulateObject(json, this);
			}
			catch (IOException ex)
			{
				// Ignore IO issues. File probably wasn't there (first run).
				OutputLogger.Instance.LogLine(LogCategory.Log, $"Failed to load settings file {Filename}: {ex.Message}");
			}
			catch (JsonException ex)
			{
				// Problem with the json, corrupted file?
				// User loses the settings unfortunately, but better than crashing.
				OutputLogger.Instance.LogLine(LogCategory.Log, $"Failed to read settings from {Filename}: {ex.Message}");
			}
		}

		// Save the settings out to disk.
		public void Save()
		{
			var json = JsonConvert.SerializeObject(this, Formatting.Indented);
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(Filename));
				File.WriteAllText(Filename, json);
			}
			catch (IOException ex)
			{
				// Don't crash if there was some IO error.
				OutputLogger.Instance.LogLine(LogCategory.Log, $"Failed to save settings to file {Filename}: {ex.Message}");
			}
		}
	}
}
