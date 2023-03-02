using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Asistent_v1._1._2._CS._5._Settings;
/// <summary>
/// Class storing description of setting files
/// </summary>
/// <typeparam name="SettingType"></typeparam>
public class SettingsTestClass {

	public SettingsTestClass(string settingName, string settingValue) {
		this.settingName = settingName;
		this.settingValue = settingValue;
	}
	public string settingName { get; set; }

	public string settingValue { get; set; }

}
