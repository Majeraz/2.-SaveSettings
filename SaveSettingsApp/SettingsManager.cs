using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Project_Asistent_v1._1._2._CS._5._Settings;
using SaveSettingsApp;

namespace JSONFilesManagerProj;

/// <summary>
/// Get acces to JSON File at specified related file path (if it doesn't exist yet it is becoming created).
/// Gives methods to manipulate settings files.
/// </summary>
/// <typeparam name="ObjectType">Type of stored object in JSON file</typeparam>
public class SettingsManager<ObjectType> {
	private string JSONFullFilePath;
	private bool ifJSONShouldBeIEnumerable = false;
	public SettingsManager(string JSONFileRelativePath) {
        if (typeof(ObjectType).GetInterfaces().Contains(typeof(IEnumerable))) ifJSONShouldBeIEnumerable = true;
        JSONFullFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, JSONFileRelativePath);
        JSONFilesManager2.CreateJSONFileAndItsDirectory(JSONFullFilePath);
    }
	public void AddObjectToJSONFile(object objectToBeWritten) {
        if (ifJSONShouldBeIEnumerable)
            if (objectToBeWritten.GetType() != typeof(ObjectType))   // if type of file that is supposed to be added to JSON doesn't match type that is placed in JSON 
                throw new Exception("Type of the object doesn't match JSON type");
        else
            JSONFilesManager2.WriteObjectToJSONFile(JSONFullFilePath, objectToBeWritten);
	}
	/// <summary>
	/// Deletes JSON file and then rewrite it about new object. Always store full object before use this method.
	/// </summary>
	/// <param name="objectToBeWritten"></param>
	public void RewriteSetting(object objectToBeWritten) {
		File.WriteAllText(JSONFullFilePath, "");	// Clear the file
		AddObjectToJSONFile(objectToBeWritten);
	}
    public ObjectType GetSetting() {
        return JSONFilesManager2.DeserializeJSON<ObjectType>(JSONFullFilePath);
    }

    /// <summary>
    /// Extends JSON file about list of T or T.
    /// </summary>
    /// <param name="objectToBeWritten"></param>
    /// <returns></returns>
}
