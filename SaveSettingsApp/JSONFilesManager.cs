using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace SaveSettingsApp {
	public static class JSONFilesManager {

		// Set up your files names
		/// <summary>
		/// Relative to dll location
		/// </summary>
		public static string JSONFileRealiveDirectory = "Settings";
		public static string JSONFileName = "settings.json";

		private static string dllFolderFullPath;
		private static string settingsFileFullPath;
		private static string settingsFolderFullPath;

		public static List<T> GetJSONFile<T>(string JSONFilePath) {
			return DeserializeObject<T>(JSONFilePath);
		}

		public static string AppendJSONFile(object appendingObject, string JSONFileRealiveDirectory, string JSONFileName) {
			string JSONFullFilePath = GetJSONFileAndItsDirectory(typeof(JSONFilesManager), JSONFileRealiveDirectory, JSONFileName);
			AppendJSONFileAboutAnSerializedObject(JSONFullFilePath, appendingObject);
			return JSONFullFilePath;
		}

		private static void AppendJSONFileAboutAnSerializedObject(string JSONFilePath, object someObject ) {
			Action AppendJSONFile;
			if(someObject is not IEnumerable<object>) {
				someObject = new List<object>() {	// Convert to an IEnumerable object
							someObject
				};
			}
			string serializedObject = SerializeObject(someObject);
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
			serializedObject = serializedObject.Remove(0, 5).Insert(0, Environment.NewLine).Insert(0, ",");
		}

		private static void Cut2LastCharactersFromJSONFile(string jSONFilePath) {
			using(FileStream fs = File.OpenWrite(jSONFilePath)) {
				fs.SetLength(fs.Seek(-3, SeekOrigin.End));
			}
		}
		/// <summary>
		/// If file doesn't exist then create it and its directory, returns JSONFileFullPath
		/// </summary>
		/// <param name="JSONFilePath"></param>
		private static string GetJSONFileAndItsDirectory(Type typeOfThisClass, string JSONFileRealiveDirectory, string JSONFileName) {
			dllFolderFullPath = Path.GetDirectoryName(typeOfThisClass.Assembly.Location)!;
			string JSONFileFullPath = Path.Combine(dllFolderFullPath, JSONFileRealiveDirectory, JSONFileName);

			if(!File.Exists(JSONFileFullPath)) {
				Directory.CreateDirectory(Path.Combine(dllFolderFullPath, JSONFileRealiveDirectory));
				// Create JSON settings file
				using(FileStream fs = File.Create(JSONFileFullPath)) ;
			}
			return JSONFileFullPath;
		}

		private static string SerializeObject(object someObject) {
			string myJSONString = Newtonsoft.Json.JsonConvert.SerializeObject(someObject, Newtonsoft.Json.Formatting.Indented);
			return myJSONString;
		}
		private static List<T> DeserializeObject<T>(string JSONFilePath) {
			string deserializedJSON = File.ReadAllText(JSONFilePath);
			List<T> objectList = JsonSerializer.Deserialize<List<T>>(deserializedJSON);
			return objectList;
		}
	}
}
