//http://stackoverflow.com/questions/217902/reading-writing-an-ini-file
using System;
using System.IO;
using System.Collections;

public class IniParser
{
	private Hashtable keyPairs = new Hashtable();
    private String iniFilePath;

    private struct SectionPair
    {
        public String Section;
        public String Key;
    }
	public IniParser(String iniPath)
    {
	TextReader iniFile = null;
	String strLine = null;
	String currentRoot = null;
	String[] keyPair = null;

	iniFilePath = iniPath;	
	}
	public String GetSetting(String sectionName, String settingName)
    {
        SectionPair sectionPair;
        sectionPair.Section = sectionName.ToUpper();
        sectionPair.Key = settingName.ToUpper();

        return (String)keyPairs[sectionPair];
    }

}	