using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveSettingsApp {
	public static class SaveSettingsClass {

		private static string settingsFolderName;
		private static string settingsJSONFileName;

		private static string dllFolderFullPath;
		private static string settingsFileFullPath;
		private static string settingsFolderFullPath;

		public static void SaveSettings() {
			// Set up your files names
			settingsFolderName = "Settings";
			settingsJSONFileName = "settings.json";

			GetAllPaths(typeof(SaveSettingsClass));
			if(!SettingsJSONFileExists())
				CreateSettingsFolderAndJSONFile();
			// Override JSON file

		}

		private static bool SettingsJSONFileExists() {
			if(File.Exists(settingsFileFullPath))
				return true;
			return false;
		}

		/// <summary>
		/// Get paths to: dll, settings folder, settingsJSON file
		/// </summary>
		/// <param name="typeOfThisClass"></param>
		/// <returns></returns>
		private static string GetAllPaths(Type typeOfThisClass) {
			dllFolderFullPath = Path.GetDirectoryName(typeOfThisClass.Assembly.Location)!;
			// Variable for checking existence, eventually creating, the settings JSON file
			settingsFileFullPath = Path.Combine(dllFolderFullPath, settingsFolderName, settingsJSONFileName);
			// Variable for method for create folder at desired name
			settingsFolderFullPath = Path.Combine(dllFolderFullPath, settingsFolderName);
			return dllFolderFullPath;
		}
		private static void CreateSettingsFolderAndJSONFile() {
				// Create directory but if exists do nothing
				Directory.CreateDirectory(settingsFolderFullPath);
				// Create JSON settings file
				File.Create(settingsFileFullPath);
			}
		}
	}
}
