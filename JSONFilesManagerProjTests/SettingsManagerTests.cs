using Microsoft.VisualStudio.TestTools.UnitTesting;
using JSONFilesManagerProj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;

namespace JSONFilesManagerProj.Tests;
public class MySubassembly {
    public List<object> components { get; set; }
    public MySubassembly() {
        components = new();
    }

}
public class MyProject {
    public List<object> subassemblyList { get; set; }
    public MyProject() {
        subassemblyList = new();
    }
}

[TestClass()]
public class SettingsManagerTests {
    public SettingsManagerTests() {
    }

    [TestMethod()]
    public void ConvertJObjectTypeToTheTypeIfOccuredTest_ObjectIsString() {
        string testString = "test";
        SettingsManager<int> settingsManager = new();
        settingsManager.ConvertJObjectTypeToTheTypeIfOccured(testString);
        Assert.IsTrue(testString == "test");
    }
    [TestMethod()]
    public void ConvertJObjectTypeToTheTypeIfOccuredTest_ObjectIsListOfStrings() {
        List<string> testStringList = new() { "test1", "test1" };
        List<string> originalList = new() { "test1", "test1" };
        SettingsManager<int> settingsManager = new();
        settingsManager.ConvertJObjectTypeToTheTypeIfOccured(testStringList);
        bool notTrueHelperBool = false;
        for (int i = 0; i < testStringList.Count; i++) {
            if (testStringList[i] != originalList[i])
                notTrueHelperBool = true;
        }
        Assert.IsFalse(notTrueHelperBool);
    }
    [TestMethod()]
    public void ConvertJObjectTypeToTheTypeIfOccuredTest_ObjectIsListOfClassAtSpecifiedType() {
        MyProject myProject = new();    // Create new project
        MySubassembly mySubassembly = new();    // Create new Subassembly
        List<MySubassembly> mySubassemblyList = new() { mySubassembly };    // Create mySubassemblyList
        string componentString = "componentStringValue";    // Create a string member for mySubassemblyList[0].components
        MySubassembly mySubassemblySubassembliedInComponents = new();   // Create a MySubassembly member for mySubassemblyList[0].components
        mySubassemblyList[0].components = new() { componentString, mySubassemblySubassembliedInComponents };    // Add those members to mySubassemblyList[0].components
        JObject jObject = new() { };
        mySubassemblySubassembliedInComponents.components.Add(jObject); // 

        List<string> testStringList = new() { "test1", "test1" };
        List<string> originalList = new() { "test1", "test1" };
        SettingsManager<int> settingsManager = new();
        settingsManager.ConvertJObjectTypeToTheTypeIfOccured(testStringList);
        bool notTrueHelperBool = false;
        for (int i = 0; i < testStringList.Count; i++) {
            if (testStringList[i] != originalList[i])
                notTrueHelperBool = true;
        }
        Assert.IsFalse(notTrueHelperBool);
    }
}