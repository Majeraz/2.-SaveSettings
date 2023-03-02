using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaveSettingsApp;
using SaveSettingsApp._1._KindsOfSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveSettingsApp.Tests;

[TestClass()]
public class JSONFilesManagerTests {
	static string JSONFileName = "settings.json";
	static string JSONFileRealiveDirectory = "Settings\\lala\\lala2\\";
	static string JSONFullPath = Path.Combine(Path.GetDirectoryName(typeof(JSONFilesManagerTests).Assembly.Location), JSONFileRealiveDirectory, JSONFileName);
	static string JSONFileDirectory = JSONFullPath.Remove(JSONFullPath.LastIndexOf("\\"));

	static List<SettingObjectExampleClass> generalSettingList = new() {
		 new("settingName1"),
		 new("settingName2" )
	};

	[TestMethod()]
	public void CreateJSONFileTest_CreateAllDirectoriesAndAddFileAndAddContent_BasingOnlyOnFullFilePath() {
		if(Directory.Exists(JSONFileDirectory)) {
			foreach(string i in Directory.GetFiles(JSONFileDirectory)) {
				File.Delete(i);
			}
			Directory.Delete(JSONFileDirectory);
		}
		JSONFilesManager.RewriteJSONFile<JSONFilesManagerTests>(generalSettingList, JSONFullPath);
		Assert.IsTrue(File.Exists(JSONFullPath));
		// I checked the content. It is also correct.
	}

	//[TestMethod()]
	//public void CreateJSONFileTest1() {
	//	if(Directory.Exists(JSONFileDirectory)) {
	//		foreach(string i in Directory.GetFiles(JSONFileDirectory)) {
	//			File.Delete(i);
	//		}
	//		Directory.Delete(JSONFileDirectory);
	//	}
	//	string JSONFullFilePath = JSONFilesManager.CreateJSONFile(generalSettingList, JSONFileRealiveDirectory, JSONFileName);
	//	Assert.IsTrue(File.Exists(JSONFullFilePath));
	//}
}