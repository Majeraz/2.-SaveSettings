using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveSettingsApp {
	public static class SaveSettingsClass {

		private static string dllFolderPath;
		private static string settingsFolderName;
		private static string settingsJSONFileName;

		public static void SaveSettings() {
			// Set up your files names
			settingsFolderName = "Settings";
			settingsJSONFileName = "settings.json";

			GetThisDllFolderPath(typeof(SaveSettingsClass));
			CreateFolderAndJSONFileIfDontExist();

		}

		private static string GetThisDllFolderPath(Type typeOfThisClass) {
			dllFolderPath = Path.GetDirectoryName(typeOfThisClass.Assembly.Location)!;
			return dllFolderPath;
		}
		private static void CreateFolderAndJSONFileIfDontExist() {
			// Variable for checking existence, eventually creating, the settings JSON file
			string settingsFileFullPath = Path.Combine(dllFolderPath, settingsFolderName, settingsJSONFileName);
			// Variable for method for create folder at desired name
			string settingsFolderFullPath = Path.Combine(dllFolderPath, settingsFolderName);

			if(!File.Exists(settingsFileFullPath)) { // if settings file doesn't exist...
													 // Create directory but if doesn't exist do nothing
				Directory.CreateDirectory(settingsFolderFullPath);
				// Create JSON settings file
				File.Create(settingsFileFullPath);
			}
		}
	}
}
