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
}