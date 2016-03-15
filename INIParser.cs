using System;
using System.Runtime.InteropServices;
using System.Text;
 
public class IniParser
{
    [DllImport("kernel32.dll")]
    private static extern int WritePrivateProfileString(string ApplicationName, string KeyName, string StrValue, string FileName);
    [DllImport("kernel32.dll")]
    private static extern int GetPrivateProfileString(string ApplicationName, string KeyName, string DefaultValue, StringBuilder ReturnString, int nSize, string FileName);
    
    private static String INIFile;
	
    public IniParser(String iniPath)
    {
    	INIFile=iniPath;
    }
 
    public static void WriteValue(string SectionName , string KeyName, string KeyValue)
    {
        WritePrivateProfileString(SectionName , KeyName, KeyValue, INIFile);
    }
 
    public static string ReadValue(string SectionName , string KeyName)
    {
        StringBuilder szStr = new StringBuilder(255);
        GetPrivateProfileString(SectionName, KeyName, "" , szStr, 255, INIFile);
        return szStr.ToString().Trim();
    }
    public String ReadString(String SectionName , String KeyName, String DefaultValue)
    {
        return ReadValue(SectionName , KeyName);
    }
}