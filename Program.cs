﻿//EXT1 - Изменить проверку файла, если указан не полный путь к файлу а тольк имя
//EXT2 - Включить в ScriptFinish сообщение и код возврата
//EXT3 - Рекурсивный поиск INI файла вверх и в стороны.
//
//->Доступ к файлам сервера по FTP
/*
 * Created by SharpDevelop.
 * User: Andrew
 * Date: 14.03.2016
 * Time: 22:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.IO;



namespace MySMcompiler
{
	
	
	class Program
	{
		//Global 
		static string mySMcomp_Folder; //Gets the directory where the application is stored.	
		static string SourceFile; //Source programm file .sp
		static string SourceFolder;
		static string INIFile="smcmphlp.ini"; //INI file
		static string INIFolder; //path INI file		
		//Ini file fields
		static string Compilator = "spcomp.exe";
		static string Compilator_Folder="c:\\Users\\skorik\\Documents\\SourceMod\\smK64t\\sourcemod-1.7.3-git5301\\";
		static string Compilator_Params = "vasym=\"1\" -O2";
		static string Compilator_Include_Folders = "smk64t\\include";				
		static string Plugin_Author;
		static string rcon_Address = "127.0.0.1";
		static int rcon_Port = 27015;
		static string rcon_password;		
		static string SRCDS_Folder;
		static string SMXFolder;
		
 
		public static void Main(string[] args)
		{
			Console.WriteLine("My SourceMod Compiler Helper v 2.0");
			Console.WriteLine("Compiler shell for SourceMod by Skorik 2016");
			Console.WriteLine("-------------------------------------------");
			mySMcomp_Folder=AppDomain.CurrentDomain.BaseDirectory;
			// or			
			//Application.ExecutablePath;
			//Assembly.GetExecutingAssembly().Location;
			//Application.StartupPath;
			Debug.Print("mySMcomp_Folder=" + mySMcomp_Folder);
			Debug.Print("Args count=" + args.Length);			
			//
			//Parsing argumets
			//
			if (args.Length < 1) {
				Console.WriteLine("Usage: MySMcompiler <path\file.sp>");
				ScriptFinish(true);
				System.Environment.Exit(0);
			}			
			Console.Title = Console.Title + " " + args[0] + " ";// + DateAndTime.Now;
			SourceFile = args[0];			
			Debug.Print("SourceFile=" + SourceFile);
			Console.WriteLine("Source file \t"+ SourceFile);
			
			
			//EXT1
			if (!File.Exists(SourceFile))
			{
				Console.WriteLine("File \t\t"+SourceFile+" not found");
				ScriptFinish(true);
				System.Environment.Exit(1);
			}			
			
			SourceFolder=System.IO.Directory.GetParent(SourceFile).ToString()+"\\";
			SourceFile =Path.GetFileNameWithoutExtension(SourceFile);
			Console.WriteLine("Source folder \t"+ SourceFolder);
			//EXT3
			INIFolder=SourceFolder;
			INIFile=INIFolder+"\\"+INIFile;
			if (!File.Exists(INIFile))
			{
				Console.WriteLine("INI File \t"+INIFile+" not found");
				ScriptFinish(true);
				System.Environment.Exit(2);
			}	
			Console.WriteLine("INI File \t"+ INIFile);
			GetConfigFile(INIFile);			
			//Parsing include from INI file
			string[] Compilator_Include_Folder = Compilator_Include_Folders.Split(';');
			Compilator_Include_Folders = "";
			Console.WriteLine("Include:");
			foreach (string s in Compilator_Include_Folder) 
			{
				if (!String.IsNullOrEmpty(s)) 
					{
					Console.WriteLine(s);
					Compilator_Include_Folders = Compilator_Include_Folders + " -i" + Compilator_Folder + s.Trim();
					}
			}
			//
			//Create include file
			//	
			string curDate=DateTime.Now.ToString();
			System.IO.StreamWriter f_inc = new System.IO.StreamWriter(SourceFolder + "datetimecomp.inc", true);
			f_inc.WriteLine("#if defined DEBUG");
			f_inc.WriteLine("\t#define PLUGIN_DATETIME \"" + curDate + "\"");
			f_inc.WriteLine("\t#if defined PLUGIN_VERSION");
			f_inc.WriteLine("\t\t#undef PLUGIN_VERSION");
			f_inc.WriteLine("\t#endif");
			f_inc.WriteLine("\t#define PLUGIN_VERSION \"" + curDate + "\"");
			f_inc.WriteLine("#endif");
			f_inc.WriteLine("#if !defined PLUGIN_NAME");
			f_inc.WriteLine("\t#define PLUGIN_NAME \"" + SourceFile + "\"");
			f_inc.WriteLine("#endif");
			f_inc.WriteLine("#if !defined PLUGIN_AUTHOR");
			f_inc.WriteLine("\t#define PLUGIN_AUTHOR \"" + Plugin_Author + "\"");
			f_inc.WriteLine("#endif");
	
			f_inc.Close();
			//Delete old err smx files				
			if (!File.Exists(SourceFile+".err"))File.Delete(SourceFile+".err");
			if (!File.Exists(SMXFolder+SourceFile+".smx"))File.Delete(SMXFolder+SourceFile+".smx");	
			
			//Test compiler file exist
			if (!File.Exists(Compilator_Folder + Compilator)) 
			{
				Console.WriteLine("ERR: File compiler\t" + Compilator_Folder + Compilator + " not found");
				ScriptFinish(true);
				System.Environment.Exit(4);
			}
			//Compiling
			Console.WriteLine("\nCompiling\n");
		
			Process compiler = new Process();
			compiler.StartInfo.FileName = Compilator_Folder + Compilator;
			compiler.StartInfo.Arguments = SourceFolder + SourceFile + ".sp " + Compilator_Params + " -e" + SourceFile + ".err" + " -D" + SourceFolder + " -o" + SMXFolder + SourceFile + " -w213" + Compilator_Include_Folders;
			Console.WriteLine(compiler.StartInfo.FileName + " " + compiler.StartInfo.Arguments);
				
			compiler.StartInfo.UseShellExecute = false;
			compiler.StartInfo.RedirectStandardOutput = true;
			compiler.Start();
			Console.WriteLine(compiler.StandardOutput.ReadToEnd());
			compiler.WaitForExit();	
			//ERRORLEVEL			
			if (compiler.ExitCode > 0)
			{
				Console.WriteLine(compiler.ExitCode);
				Console.WriteLine(SourceFile+".err");
				try 
				{					
			        using (StreamReader sr = new StreamReader(SourceFile+".err"))
			        {
	                String line = sr.ReadToEnd();
	                Console.WriteLine(line);
			        }
            	}        	
	        	catch (Exception e)
	        	{
	        	    Console.WriteLine("The file {0} could not be read",SourceFile+".err");
	        	}							
				
				ScriptFinish(true);
				System.Environment.Exit(5);
			}			
			
			//
			// Copy to server
			//
					
			Console.WriteLine("Finish");
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	//****************************************************	
	public static void ScriptFinish(bool pause){
	//****************************************************			
		if (pause) {
			Console.WriteLine();
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	}
	
	public static void GetConfigFile(string ConfigFile)
	{

		IniParser INI = new IniParser(ConfigFile);
		/*if (!String.IsNullOrEmpty(ConfigFile)) {			
			//http://msdn.microsoft.com/en-us/library/system.string.isnullorempty.aspx
			Console.WriteLine("INF: Use ini file " + ConfigFile);

			Inifile inifile = new Inifile(ConfigFile);
			Compilator = inifile.LoadString("Compiler", "Compilator", Compilator);
			Compilator_Folder = inifile.LoadString("Compiler", "Compilator_Folder", mySMcomp_Folder);
			Compilator_Params = inifile.LoadString("Compiler", "Parameters", Compilator_Params);
			Compilator_Include_Folders = inifile.LoadString("Compiler", "Include", Compilator_Include_Folders);
			SRCDS_Folder = inifile.LoadString("Server", "SRCDS_Folder");
			rcon_Address = inifile.LoadString("Server", "rcon_address", rcon_Address);
			rcon_Port = inifile.LoadInteger("Server", "rcon_port", rcon_Port);
			rcon_password = inifile.LoadString("Server", "rcon_password", rcon_password);
			Plugin_Author = inifile.LoadString("Compiler", "Plugin_Author", "");

		}
		Debug.Print("Compilator\t\t=" + Compilator);
		Debug.Print("Compilator_Folder\t=" + Compilator_Folder);
		Debug.Print("Compilator_Params\t=" + Compilator_Params);
		Debug.Print("Compilator_Include_Folders=" + Compilator_Include_Folders);
		Debug.Print("SRCDS_Folder=" + SRCDS_Folder);
		Debug.Print("rcon_address=" + rcon_Address);
		Debug.Print("rcon_port=" + rcon_Port);
		Debug.Print("rcon_password=" + rcon_password);*/

		//[Compiler]
		//Compilator="spcomp.exe"
		//Compilator_Folder="c:\pro\SourceMod\"
		//Parameters=
		//Include=
		//
		//[Server]
		//rcon_address="deploy2"
		//rcon_port=27015
		//rcon_password="PI"
		//SRCDS_Folder="\\deploy2\c$\Program Files\srcds.css.75\css\cstrike\" 
		//SRCDS_FTP=

	}
	}
}