using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;
using Project_Asistent_v1._1._2._CS._5._Settings;

namespace SaveSettingsApp {
	public static class JSONFilesManager {

		// Set up your files names
		/// <summary>
		/// Relative to dll location
		/// </summary>
		public static string JSONFileName = "settings.json";
		public static string JSONFileRealiveDirectory = "Settings\\lala\\lala2\\";
		public static string JSONFullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), JSONFileRealiveDirectory, JSONFileName);
		public static string JSONFileDirectory = JSONFullPath.Remove(JSONFullPath.LastIndexOf("\\"));

		/// <summary>
		/// Returns a list of specific objects, of a JSON file
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="JSONFilePath"></param>
		/// <returns></returns>
		//public static List<T> GetJSONFileAsAnGenericList<T>(string JSONFilePath) {
		//	return DeserializeObject<T>(JSONFilePath);
		//}
		/// <summary>
		/// Returns a list of specific objects, of a JSON file
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="JSONFilePath"></param>
		/// <returns></returns>
		public static List<SettingsTestClass> GetJSONFileAsAnGenericList(string JSONFileRealiveDirectory, string JSONFileName) {
			string dllFolderFullPath = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))!;
			string JSONFileFullPath = Path.Combine(dllFolderFullPath, JSONFileRealiveDirectory, JSONFileName);
			return DeserializeObject(JSONFileFullPath);
		}


		/// <summary>
		/// Append list<T> independently if an appending object is T or list<T>
		/// </summary>
		/// <param name="appendingObject"></param>
		/// <param name="JSONFileRealiveDirectory"></param>
		/// <param name="JSONFileName"></param>
		/// <returns></returns>
		public static void AppendJSONFile(object appendingObject, string JSONFullFilePath) {
			AppendJSONFileAboutAnSerializedObject(JSONFullFilePath, appendingObject);
		}

		/// <summary>
		/// Creates or rewrites a JSONFile of specified name and at relative,
		/// to dll directory (of class passed as generic type), location. It creates necessary directories and files if they are not exist and write passed object into it.
		/// </summary>
		/// <param name="objectToBeWritten"></param>
		/// <param name="JSONFileRealiveDirectory"></param>
		/// <param name="JSONFileName"></param>
		/// <returns></returns>
		public static string RewriteJSONFile(object objectToBeWritten, string JSONFileRealiveDirectory, string JSONFileName) {
			string JSONFullFilePath = GetJSONFullFilePathAndCreateItsDirectory(JSONFileRealiveDirectory, JSONFileName);
			using(File.Create(JSONFullFilePath)) ;
			AppendJSONFileAboutAnSerializedObject(JSONFullFilePath, objectToBeWritten);
			return JSONFullFilePath;
		}
		/// <summary>
		/// Creates or rewrites a JSONFile basing on passed file path. It creates necessary directories and files if they are not exist and write passed object into it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectToBeWritten"></param>
		/// <param name="JSONFullFilePath"></param>
		/// <returns></returns>
		public static string RewriteJSONFile<T>(object objectToBeWritten, string JSONFullFilePath) {
			GetJSONFileAndItsDirectory(typeof(T), JSONFullFilePath);
			using(File.Create(JSONFullFilePath));
			AppendJSONFileAboutAnSerializedObject(JSONFullFilePath, objectToBeWritten);
			return JSONFullFilePath;
		}




		private static void AppendJSONFileAboutAnSerializedObject(string JSONFullFilePath, object objectToBeWritten ) {
			Action AppendJSONFile;
			if(objectToBeWritten is not IEnumerable<object>) {
				objectToBeWritten = new List<object>() {	// Convert to an IEnumerable object
							objectToBeWritten
				};
			}
			string serializedObject = SerializeObject(objectToBeWritten);
			using(FileStream fs = File.OpenRead(JSONFullFilePath)) {
				if(fs.Length == 0) {
					AppendJSONFile = () => { File.AppendAllText(JSONFullFilePath, serializedObject); };
				} else {
					AppendJSONFile = () => {
						Cut2LastCharactersFromJSONFile(JSONFullFilePath);
						Cut2FirstCharactersFromSerializedObject(ref serializedObject);
						File.AppendAllText(JSONFullFilePath, serializedObject);
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
		private static string GetJSONFullFilePathAndCreateItsDirectory(string JSONFileRealiveDirectory, string JSONFileName) {
			string dllFolderFullPath = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))!;
			string JSONFileFullPath = Path.Combine(dllFolderFullPath, JSONFileRealiveDirectory, JSONFileName);
			Directory.CreateDirectory(Path.Combine(dllFolderFullPath, JSONFileRealiveDirectory));
			if(!File.Exists(JSONFileFullPath)) {
				// Create JSON settings file
				using(FileStream fs = File.Create(JSONFileFullPath)) ;
			}
			return JSONFileFullPath;
		}
		/// <summary>
		/// If file doesn't exist then create it and its directory, returns JSONFileFullPath
		/// </summary>
		/// <param name="JSONFilePath"></param>
		private static void GetJSONFileAndItsDirectory(Type typeOfThisClass, string JSONFullFilePath) {
			string realtiveDirectory = JSONFullFilePath.Remove(JSONFullFilePath.LastIndexOf("\\"));
			Directory.CreateDirectory(realtiveDirectory);
			if(!File.Exists(JSONFullFilePath)) {
				// Create JSON settings file
				using(FileStream fs = File.Create(JSONFullFilePath));
			}
		}

		private static string SerializeObject(object someObject) {
			string myJSONString = Newtonsoft.Json.JsonConvert.SerializeObject(someObject, Newtonsoft.Json.Formatting.Indented);
			return myJSONString;
		}
		private static List<SettingsTestClass> DeserializeObject(string JSONFilePath) {
			string deserializedJSON = File.ReadAllText(JSONFilePath);
			List<SettingsTestClass> objectList = JsonSerializer.Deserialize<List<SettingsTestClass>>(deserializedJSON);
			return objectList;
		}
	}
}
