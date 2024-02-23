using JSONFilesManagerProj;



// Files examples 
string JSONFileRelativeDirectoryPath = "Settings\\"; // Define a relative directory of the *.json file
string JSONFileRelativePath = JSONFileRelativeDirectoryPath + "testSetting.json";   // Define a name for the file

// DEBUG TO SEE HOW IT WORKS
// Code usage example for manipulate JSON files

List<string> myStringList = new();


SettingsManager<List<string>> settingManager = new(JSONFileRelativePath, ref myStringList); // Pass object to be supported by settings manager
myStringList.Add("test");   // Modify the object part 1/2
myStringList.Add("test2");   // Modify the object part 2/2
settingManager.RewriteSetting();    // Rewrite settings