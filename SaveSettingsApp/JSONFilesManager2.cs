using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Project_Asistent_v1._1._2._CS._5._Settings;

namespace JSONFilesManagerProj;
public class JSONFilesManager2 {
	#region Private methods
	private static void Cut2FirstCharactersFromSerializedObject(ref string serializedObject) {
		serializedObject = serializedObject.Remove(0, 5).Insert(0, Environment.NewLine).Insert(0, ",");
	}
	private static void Cut2LastCharactersFromJSONFile(string jSONFilePath) {
		using(FileStream fs = File.OpenWrite(jSONFilePath)) {
			fs.SetLength(fs.Seek(-3, SeekOrigin.End));
		}
	}
	#endregion

	#region Public methods
	/// <summary>
	/// Creates JSON file and its directory if they not exist. If they exist, method just returns fullJSONFilePath.
	/// </summary>
	/// <param name="JSONFileName"></param>
	/// <param name="JSONFileRealiveDirectory"></param>
	/// <returns></returns>

	/// <summary>
	/// Add object or List<object> to JSON.
	/// </summary>
	/// <param name="JSONFullFilePath"></param>
	/// <param name="objectToBeWritten"></param>
	public static void AddObjectToJSON<ObjectType>(string JSONFileRelativePath, object objectToBeWritten){
		string JSONFullFilePath = CreateJSONFileAndItsDirectory(JSONFileRelativePath);
		List<ObjectType> objectList = DeserializeJSON<ObjectType>(JSONFileRelativePath);	// Deserialize current JSONFile
		if(objectToBeWritten is not IEnumerable<object>) {
			objectList.Add((ObjectType)objectToBeWritten);	// If it is just a single object, then add it
		} else {
			objectList.AddRange((List<ObjectType>)objectToBeWritten);	// If it is list of objects, then add its range
		}
		string serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(objectList, Newtonsoft.Json.Formatting.Indented);
		File.AppendAllText(JSONFullFilePath, serializedObject);
	}

	public static List<ObjectType> DeserializeJSON<ObjectType>(string JSONFileRelativePath) {
		List<ObjectType> objectList = new();
		string JSONFullFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), JSONFileRelativePath);
		string deserializedJSON = File.ReadAllText(JSONFullFilePath);
			if(deserializedJSON.Length > 0) {
			objectList = JsonSerializer.Deserialize<List<ObjectType>>(deserializedJSON);
		}
		return objectList;
	}

	private static string CreateJSONFileAndItsDirectory(string JSONFileRelativePath) {
		string JSONFullFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), JSONFileRelativePath);
		Directory.CreateDirectory(JSONFullFilePath.Remove(JSONFullFilePath.LastIndexOf("\\")));
		if(!File.Exists(JSONFullFilePath)) {
			// Create JSON settings file
			using(FileStream fs = File.Create(JSONFullFilePath)) ;
		}
		return JSONFullFilePath;
	}
	#endregion


}