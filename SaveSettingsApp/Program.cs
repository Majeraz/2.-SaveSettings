using JSONFilesManagerProj;



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