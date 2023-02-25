using SaveSettingsApp;
using SaveSettingsApp._1._KindsOfSettings;

#region Examples of paths and names
string JSONFileRealiveDirectory = "Settings\\lala\\lala2";
string JSONFileName = "settings.json";
string JSONFullPath = Path.Combine(typeof(Program).Assembly.Location, JSONFileRealiveDirectory, JSONFileName);
#endregion

#region Objects of some type for Tests
GeneralSetting gs = new("settingName3AsNotIEnumerable");
List<GeneralSetting> generalSettingList= new() {
	 new("settingName1"),
	 new("settingName2")
};
#endregion

#region Code usage example
string JSONFullFilePath = JSONFilesManager.CreateJSONFile<Program>(generalSettingList, JSONFileRealiveDirectory, JSONFileName);
JSONFilesManager.AppendJSONFile(generalSettingList, JSONFileRealiveDirectory, JSONFileName);
List<GeneralSetting> myList = JSONFilesManager.GetJSONFileAsAnGenericList<GeneralSetting>(JSONFullFilePath); ;
#endregion




//var myTextSettingsFileName = StaticResources.dataBaseFileName;
//var myDataFileFileFullPath = dllFolderPath + myTextSettingsFileName;
//File.WriteAllText(myDataFileFileFullPath, "lala");
//if(File.Exists(myDataFileFileFullPath)) {
//	StoreTXTFileInStringList();
//	return true;
//}
//return false;
//}
//public static void StoreTXTFileInStringList() {
//}