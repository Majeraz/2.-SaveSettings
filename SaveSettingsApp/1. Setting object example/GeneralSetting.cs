using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveSettingsApp._1._KindsOfSettings {
	public class SettingObjectExampleClass {
		public SettingObjectExampleClass(string settingName) {
			SettingName = settingName;
		}
		public string SettingName { get; set; }
		public string? MyName2 { get; set; }
		public bool MyBool1 { get; set; }
		public double MyDouble { get; set; }
	}
}
