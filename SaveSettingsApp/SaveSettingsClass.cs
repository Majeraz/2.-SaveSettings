using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JSONFilesManipulation;

namespace SaveSettingsApp {
	public static class SaveSettingsClass {

		// Set up your files names
		private static string settingsFolderName = "Settings";
		private static string settingsJSONFileName = "settings.json";

		private static string dllFolderFullPath;
		private static string settingsFileFullPath;
		private static string settingsFolderFullPath;

		public static void SaveSettings(object settingObject) {
			GetAllPaths(typeof(SaveSettingsClass));
			if(!ifSettingsJSONFileExists()) {
				CreateSettingsFolderAndJSONFile();
			}
			AppendJSONFileAboutAnSerializedObject(settingsFileFullPath, settingObject);
			// Override JSON file

		}

		private static void AppendJSONFileAboutAnSerializedObject(string JSONFilePath, object someObject ) {
			JSONFilesManipulationClass fileManipulation = new();
			Action AppendJSONFile;
			if(someObject is not IEnumerable<object>) {
				someObject = new List<object>() {	// Convert to an IEnumerable object
							someObject
				};
			}
			string serializedObject = fileManipulation.SerializeObject(someObject);
			using(FileStream fs = File.OpenRead(JSONFilePath)) {
				if(fs.Length == 0) {
					AppendJSONFile = () => { File.AppendAllText(JSONFilePath, serializedObject); };
				} else {
					AppendJSONFile = () => {
						Cut2LastCharactersFromJSONFile(JSONFilePath);
						Cut2FirstCharactersFromSerializedObject(ref serializedObject);
						File.AppendAllText(JSONFilePath, serializedObject);
					};
				}
			}
			AppendJSONFile?.Invoke();
		}

		private static void Cut2FirstCharactersFromSerializedObject(ref string serializedObject) {
			serializedObject = serializedObject.Remove(0,1).Insert(0,",");
		}

		private static void Cut2LastCharactersFromJSONFile(string jSONFilePath) {
			using(FileStream fs = File.OpenWrite(jSONFilePath)) {
				fs.SetLength(fs.Seek(-1, SeekOrigin.End));
			}
		}

		private static void CreateSettingsFolderAndJSONFile() {
			// Create directory but if exists do nothing
			Directory.CreateDirectory(settingsFolderFullPath);
			// Create JSON settings file
			FileStream fs = File.Create(settingsFileFullPath);
			fs.Close();
		}
		private static bool ifSettingsJSONFileExists() {
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


	}
}
