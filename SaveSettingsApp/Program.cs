

using SaveSettingsApp;
using SaveSettingsApp._1._KindsOfSettings;

GeneralSetting gs = new("settingName3AsNotIEnumerable");
List<GeneralSetting> geneeralSettingList= new() {
	 new("settingName1"),
	 new("settingName2")
};
SaveSettingsClass.SaveSettings(gs);





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