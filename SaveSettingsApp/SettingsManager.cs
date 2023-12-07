using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace JSONFilesManagerProj;

/// <summary>
/// Get acces to JSON File at specified related file path (if it doesn't exist yet it is becoming created).
/// Gives methods to manipulate settings files.
/// </summary>
/// <typeparam name="ObjectType">Type of stored object in JSON file</typeparam>
public class SettingsManager<ObjectType> {
	private string JSONFullFilePath;
    private ObjectType referenceToTheOriginalObject;
    public SettingsManager(string JSONFileRelativePath, ref ObjectType originalObject) {
        JSONFullFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, JSONFileRelativePath);
        JSONFilesManager.CreateJSONFileAndItsDirectory(JSONFullFilePath);
        referenceToTheOriginalObject = GetSetting();
        originalObject = referenceToTheOriginalObject!;
    }

    public void AddObjectToJSONFile(object objectToBeWritten) {
        if (objectToBeWritten.GetType() != typeof(ObjectType))   // if type of file that is supposed to be added to JSON doesn't match type that is placed in JSON 
                throw new Exception("Type of the object doesn't match JSON type");
        JSONFilesManager.WriteObjectToJSONFile(JSONFullFilePath, objectToBeWritten);
	}
	/// <summary>
	/// Udates JSON file for the object associated with this SettingsManager object. It can be used for initialize new JSON file.
	/// </summary>
	/// <param name="objectToBeWritten"></param>
	public void RewriteSetting() {
		File.WriteAllText(JSONFullFilePath, "");	// Clear the file
		AddObjectToJSONFile(referenceToTheOriginalObject!);
	}
    /// <summary>
    /// Store JSON file in reference referenceToTheOriginalObject (load settings)
    /// </summary>
    public ObjectType GetSetting() {
        object deserializedJSON = JSONFilesManager.DeserializeJSON<ObjectType>(JSONFullFilePath)!;
        return (ObjectType)deserializedJSON;
    }

    /// <summary>
    /// Extends JSON file about list of T or T.
    /// </summary>
    /// <param name="objectToBeWritten"></param>
    /// <returns></returns>
}
