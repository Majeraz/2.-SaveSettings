﻿using SaveSettingsApp;
using SaveSettingsApp._1._KindsOfSettings;


string JSONFileRealiveDirectory = "Settings\\lala\\lala2";
string JSONFileName = "settings.json";

GeneralSetting gs = new("settingName3AsNotIEnumerable");

List<GeneralSetting> generalSettingList= new() {
	 new("settingName1"),
	 new("settingName2")
};
string JSONFullFilePath = JSONFilesManager.AppendJSONFile(generalSettingList, JSONFileRealiveDirectory, JSONFileName);
List<GeneralSetting> myList = JSONFilesManager.GetJSONFile<GeneralSetting>(JSONFullFilePath); ;





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