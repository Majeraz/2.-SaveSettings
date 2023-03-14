﻿using System;
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
/// Get acces to JSON File at specified related file path (if it doesn't exist yet it is beeing created).
/// Gives methods to manipulate settings files.
/// </summary>
/// <typeparam name="ObjectType"></typeparam>
public class SettingsManager<ObjectType> {
	private string JSONFileRelativePath;
	private string JSONFullFilePath;
	public SettingsManager(string JSONFileRelativePath) {
		this.JSONFileRelativePath = JSONFileRelativePath;
        JSONFullFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), JSONFileRelativePath);

    }

    /// <summary>
    /// Creates or extend JSON file about list<T> or T.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public bool AddSetting(object objectToBeWritten) {
		if(objectToBeWritten is IEnumerable<object>){
			if(objectToBeWritten.GetType().GetGenericArguments().Single() != typeof(ObjectType))
				return false;
		} else {
			if(objectToBeWritten.GetType() != typeof(ObjectType))
				return false;
		}
		JSONFilesManager2.AddObjectToJSON<ObjectType>(JSONFileRelativePath, objectToBeWritten);
		return true;
	}
	public void UpdateSetting(object objectToBeWritten) {
		File.Delete(JSONFullFilePath);
		AddSetting(objectToBeWritten);
	}

	/// <summary>
	/// Reads JSON file and save it as List<T>
	/// </summary>
	/// <returns>List<T></returns>
	public List<ObjectType> GetSettings() {
		return (List<ObjectType>)JSONFilesManager2.DeserializeJSON<ObjectType>(JSONFileRelativePath);
	}

}
