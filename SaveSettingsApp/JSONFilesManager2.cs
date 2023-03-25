using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Project_Asistent_v1._1._2._CS._5._Settings;

namespace JSONFilesManagerProj;
/// <summary>
/// Creates JSON file and operate on it
/// </summary>
public static class JSONFilesManager2 {

	/// <summary>
	/// Get JSON file at specified path, deserialize it, extend and save again as List of T.
	/// </summary>
	/// <typeparam name="ObjectType"></typeparam>
	/// <param name="JSONFileFullPath"></param>
	/// <param name="objectToBeWritten"></param>
	public static void AddObjectToListOfObjectsInJSONFile<ObjectType>(string JSONFullFilePath, object objectToBeWritten){
		dynamic objectList = (DeserializeJSON<ObjectType>(JSONFullFilePath) as IEnumerable)!;	// Deserialize current JSONFile to extend it later
		if(!objectToBeWritten.GetType().GetInterfaces().Contains(typeof(IEnumerable))) {	// check if object to be written is enumerable
			objectList.Add((ObjectType)objectToBeWritten);	// If it is just a single object, then add it
		} else {
			objectList.AddRange((List<ObjectType>)objectToBeWritten);	// If it is list of objects, then add its range
		}
		string serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(objectList, Newtonsoft.Json.Formatting.Indented);
		File.WriteAllText(JSONFullFilePath, serializedObject);
	}
	/// <summary>
	/// Serialize object and write it to JSON file at specified path
	/// </summary>
	/// <param name="JSONFileFullPath"></param>
	/// <param name="objectToBeWritten"></param>
	public static void WriteObjectToJSONFile(string JSONFileFullPath, object objectToBeWritten) {
		string serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(objectToBeWritten, Newtonsoft.Json.Formatting.Indented);
		File.WriteAllText(JSONFileFullPath, serializedObject);
    }


	/// <summary>
	/// If JSON is empty then returns an empty object. Otherwise it returns deserialized JSON as object
	/// </summary>
	/// <typeparam name="ObjectType"></typeparam>
	/// <param name="JSONFullFilePath"></param>
	/// <returns></returns>
	public static ObjectType DeserializeJSON<ObjectType>(string JSONFullFilePath){
        string deserializedJSON = File.ReadAllText(JSONFullFilePath);
		if(deserializedJSON.Length > 0)
			return JsonSerializer.Deserialize<ObjectType>(deserializedJSON)!;
        return (ObjectType)Activator.CreateInstance<ObjectType>();
    }

	/// <summary>
	/// Creates JSON file and its directory if they are not exist.
	/// </summary>
	/// <param name="JSONFullFilePath"></param>
	public static void CreateJSONFileAndItsDirectory(string JSONFullFilePath) {
		Directory.CreateDirectory(Path.GetDirectoryName(JSONFullFilePath)!);	//Creates directory if not exist
		if(!File.Exists(JSONFullFilePath)) {	// Check if file already exists
			using(FileStream fs = File.Create(JSONFullFilePath));	 // If doesn't exist then create the file
		}
	}
}