using JSONFilesManagerProj;
using Project_Asistent_v1._1._2._CS._5._Settings;
using SaveSettingsApp;
using SaveSettingsApp._1._KindsOfSettings;

// Files examples 
string JSONFileRelativeDirectoryPath= "Settings\\";
string JSONFileRelativePath = JSONFileRelativeDirectoryPath + "testSetting.json";

// DEBUG TO SEE HOW IT WORKS
// Code usage example for manipulate JSON files

List<string> myStringList = new() {
	"test1",
	"test2",
};


//SettingsManager<List<string>> settingManager = new(JSONFileRelativePath, myStringList);
//settingManager.AddObjectToJSONFile(myStringList);
//settingManager.GetSettings();