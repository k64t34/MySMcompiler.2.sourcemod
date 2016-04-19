// Добавить rcon консоль
// дублировние  консоли ссервера

//EXT1 - Изменить проверку файла, если указан не полный путь к файлу а тольк имя
//EXT2 - Включить в ScriptFinish сообщение и код возврата
//EXT3 - Рекурсивный поиск INI файла вверх и в стороны.
//EXT4 - Проверять расширения
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
using System.Net;
using System.Threading;
using System.Reflection;


namespace MySMcompiler
{
	
	
	class Program
	{
		//Global 
		static string mySMcomp_Folder; //Gets the directory where the application is stored.	
		static string SourceFile; //Source programm file .sp
		static string SourceFolder;//Path to source programm file .sp
		static string INIFile="smcmphlp.ini"; //INI file
		static string PluginFolder; //Base project folder with .git 
		static string INIFolder; //path INI file		
		//Ini file fields
		static string Compilator = "spcomp.exe";
		static string Compilator_Folder;
		static string Compilator_Params = "";//"vasym=\"1\" -O2";
		static string Compilator_Include_Folders = "smk64t\\scripting\\include";				
		static string Plugin_Author;
		static string rcon_Address = "127.0.0.1";
		static int rcon_Port = 27015;
		static string rcon_password;		
		static string SRCDS_Folder;		
		static string SMXFolder="game\\addons\\sourcemod\\plugins\\";
		static bool MapReload=false; 
		
 
		public static void Main(string[] args)
		{
			string title="Sourcemod Compiler Helper ver"+(FileVersionInfo.GetVersionInfo((Assembly.GetExecutingAssembly()).Location)).ProductVersion+": ";
			Console.Title=title;
			Console.ForegroundColor=ConsoleColor.Cyan;
			Console.WriteLine(title);
			Console.WriteLine("-------------------------------------------------");			
			Console.ResetColor();

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
				Console.WriteLine("Usage: MySMcompiler <path\\file.sp>");
				ScriptFinish(true);
				System.Environment.Exit(0);
			}			
			Console.Title = title + " " + args[0] + " "+DateTime.Now.ToString();
			SourceFile = args[0];			
			Debug.Print("SourceFile=" + SourceFile);
			Console.WriteLine("Argumets \t"+ SourceFile);	
			
			//EXT1
			if (!File.Exists(SourceFile))
			{
				Console.ForegroundColor=ConsoleColor.Red;
				Console.WriteLine("ERR: File \t\t"+SourceFile+" not found");
				Console.ResetColor();
				ScriptFinish(true);
				System.Environment.Exit(1);
			}			
			
			SourceFolder=System.IO.Directory.GetParent(SourceFile).ToString()+"\\";
			CheckFolderString(ref SourceFolder);
			SourceFile =Path.GetFileNameWithoutExtension(SourceFile);
			Console.Title = title + SourceFile + ".sp "+DateTime.Now.ToString();
			//EXT4
			Console.WriteLine("Source file \t"+ SourceFile+".sp");
			Console.WriteLine("Source folder \t"+ SourceFolder);
			//EXT3
			//Поиск INI вверх по 3-ум способам:
			// 1 просто прыгнуть вверх на пару папок
			// 2 искать папку содежащую .git или addon			
			INIFolder=System.IO.Directory.GetParent(SourceFolder).ToString();
			INIFolder=System.IO.Directory.GetParent(INIFolder).ToString();
			INIFolder=System.IO.Directory.GetParent(INIFolder).ToString();			
			INIFolder=System.IO.Directory.GetParent(INIFolder).ToString();
			PluginFolder=INIFolder;
			CheckFolderString(ref PluginFolder);
			Console.WriteLine("Plugin Folder\t"+ PluginFolder);
			INIFolder=System.IO.Directory.GetParent(INIFolder).ToString();
			CheckFolderString(ref INIFolder);
			Debug.Print("INIFolder=" + INIFolder);					
			INIFile=INIFolder+INIFile;
			if (!File.Exists(INIFile))
			{
				Console.ForegroundColor=ConsoleColor.Red;
				Console.WriteLine("ERR:INI File \t"+INIFile+" not found");
				Console.ResetColor();
				ScriptFinish(true);
				System.Environment.Exit(2);
			}	
			Console.WriteLine("INI File \t"+ INIFile);
			
			Console.ForegroundColor=ConsoleColor.White;
			Console.WriteLine("\nRead config\n");
			Console.ResetColor();
			
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
					Compilator_Include_Folders +=" -i" + s.Trim();
					}
			}
			//
			//Create include file datetime.inc
			//	
			string curDate=DateTime.Now.ToString("dd.MM.yy HH:mm:ss");
			System.IO.StreamWriter f_inc = new System.IO.StreamWriter(SourceFolder + "datetimecomp.inc", false);
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
			if (File.Exists(PluginFolder+SMXFolder+SourceFile+".smx"))File.Delete(PluginFolder+SMXFolder+SourceFile+".smx");
			
			//Test compiler file exist
			if (!File.Exists(Compilator_Folder + Compilator))
			{	Console.ForegroundColor=ConsoleColor.Red;
				if (Directory.Exists(Compilator_Folder))				
				Console.WriteLine("ERR: File compiler {0} not found in folder {1}",Compilator,Compilator_Folder);					
				else
				Console.WriteLine("ERR: Folder {0} with compiler {1} not found.",Compilator_Folder,Compilator);
				Console.ResetColor();
				ScriptFinish(true);
				System.Environment.Exit(4);
			}
			//Test compiled folder exist
			if (!Directory.Exists(PluginFolder+SMXFolder))	System.IO.Directory.CreateDirectory(PluginFolder+SMXFolder);		
			//Compiling
			Console.ForegroundColor=ConsoleColor.White;
			Console.WriteLine("\nRun compiling\n");
			Console.ResetColor();
			Process compiler = new Process();
			compiler.StartInfo.FileName = Compilator_Folder + Compilator;
			compiler.StartInfo.UseShellExecute=false;	//https://msdn.microsoft.com/ru-ru/library/system.diagnostics.processstartinfo.workingdirectory(v=vs.110).aspx
			compiler.StartInfo.WorkingDirectory=PluginFolder;
			string DiffSourceFolder=FolderDifference(SourceFolder,INIFolder);
			compiler.StartInfo.Arguments =
				DiffSourceFolder + SourceFile + ".sp " +
				Compilator_Params + " -e" + DiffSourceFolder+SourceFile + ".err" + 
				" -D" + INIFolder + 
				" -o" + SMXFolder + SourceFile + 
				/*" -w213" +*/
				Compilator_Include_Folders;
			
			Console.WriteLine(compiler.StartInfo.FileName);				
			Console.WriteLine(compiler.StartInfo.Arguments);				
			compiler.StartInfo.UseShellExecute = false;
			compiler.StartInfo.RedirectStandardOutput = true;
			compiler.Start();
			Console.WriteLine(compiler.StandardOutput.ReadToEnd());
			compiler.WaitForExit();	
			//ERRORLEVEL
			ConsoleColor ERRORLEVEL_color;
			if (compiler.ExitCode>0)ERRORLEVEL_color=ConsoleColor.Red;
			if (File.Exists(SourceFolder+SourceFile+".err")) ERRORLEVEL_color=ConsoleColor.Yellow;
			else ERRORLEVEL_color=ConsoleColor.Green;
			Console.ForegroundColor=ERRORLEVEL_color;
			Console.WriteLine(compiler.ExitCode);
			Console.ResetColor();
			if (File.Exists(SourceFolder+SourceFile+".err"))
			{				
				Console.ForegroundColor=ERRORLEVEL_color;
				if (compiler.ExitCode>0)					
				Console.WriteLine("ERR: "+SourceFolder+SourceFile+".err\n--------------------------------------------------------");
				else
				Console.WriteLine("WARN: "+SourceFolder+SourceFile+".err\n--------------------------------------------------------");
				
				Console.ResetColor();
				try 
				{					
			        using (StreamReader sr = new StreamReader(SourceFolder+SourceFile+".err"))
			        {
	                String line = sr.ReadToEnd();
	                Console.WriteLine(line);
			        }
            	}        	
	        	catch (Exception e)
	        	{
	        		Console.ForegroundColor=ConsoleColor.Red;
	        	    Console.WriteLine("The file could not be read "+SourceFolder+SourceFile+".err");
	        	    Console.WriteLine(e.Message);
	        	    Console.ResetColor();
	        	}
	        	ScriptFinish(true);
				System.Environment.Exit(0);
			}	        
			//
			// Copy to server
			//
			Console.ForegroundColor=ConsoleColor.White;
			Console.WriteLine("\nCopy files to server {0}\n",SRCDS_Folder);
			Console.ResetColor();
		    if (!Directory.Exists(SRCDS_Folder)) 
		    {
				Console.ForegroundColor=ConsoleColor.Red;
		    	Console.WriteLine("ERR:Folder for copy smx file " + SRCDS_Folder + "not found");
		    	Console.ResetColor();
				ScriptFinish(true);
				System.Environment.Exit(0);
			}
			CopyDirectory(PluginFolder, SRCDS_Folder);			
			//
			// Reload plugin
			//   
			Console.ForegroundColor=ConsoleColor.White;
			Console.WriteLine("\nReload plugin {0} on server {1}:{2}\n",SourceFile,rcon_Address,rcon_Port);
			Console.ResetColor();
			SourceRcon.SourceRcon RCon = new SourceRcon.SourceRcon();
			RCon.Errors += new SourceRcon.StringOutput(ErrorOutput);
			RCon.ServerOutput += new SourceRcon.StringOutput(ConsoleOutput);
			if (RCon.Connect(new IPEndPoint(IPAddress.Parse(rcon_Address), rcon_Port), rcon_password))
			{
				while(!RCon.Connected)
				{
					Thread.Sleep(10);
				}
				RCon.ServerCommand("status");
				Thread.Sleep(1000);
				if (MapReload)
				{
				Console.WriteLine("Restart server");	
				RCon.ServerCommand("_restart");	
				Thread.Sleep(5000);
				}
				else 
				{
					RCon.ServerCommand("sm plugins unload "+SourceFile);
					Thread.Sleep(1000);
					RCon.ServerCommand("sm plugins load "+SourceFile);
				}
				Thread.Sleep(1000);				
				RCon.ServerCommand("sm plugins info "+SourceFile);				
				Thread.Sleep(1000);
				/*Console.Write("Press Esc key to exit . . . ");			
				ConsoleKeyInfo k=Console.ReadKey();
				while (k.Key!= ConsoleKey.Escape)
				{
					k=Console.ReadKey();
					RCon.ServerCommand(Console.ReadLine());
					Console.ReadLine
				}*/
				/*while(true)
				{
				RCon.ServerCommand(Console.ReadLine());
				}				*/
			}
			else
			{
				Console.ForegroundColor=ConsoleColor.Red;
				Console.WriteLine("ERR: No connection.");
				Console.ResetColor();
			}
			Thread.Sleep(1000);	
			RCon=null;
			ScriptFinish(true);
			System.Environment.Exit(0);							
			
		}
		
		
	static void ErrorOutput(string input){			Console.WriteLine("Error: {0}", input);		}

	static void ConsoleOutput(string input)	{			Console.WriteLine("Console: {0}", input);		}	
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
		IniParser inifile = new IniParser(ConfigFile);
		Compilator_Folder = inifile.ReadString("Compiler", "Compilator_Folder",mySMcomp_Folder/*"smk64t\\sourcemod-1.7.3-git5301"*/);
		CheckFolderString(ref Compilator_Folder, PluginFolder);		
		//Если Compilator_Folder не содержит в начале строки ?:\ или \ или \\, то дополнить путь PluginFolder	Compilator_Folder=INIFolder+Compilator_Folder;
		
		Plugin_Author = inifile.ReadString("Compiler", "Plugin_Author","");
		rcon_password = inifile.ReadString("Server", "rcon_password","");
		SRCDS_Folder = inifile.ReadString("Server", "SRCDS_Folder","");				
		CheckFolderString(ref SRCDS_Folder);
		MapReload = inifile.ReadBool("Server", "MapReload",false);
		rcon_Address = inifile.ReadString("Server", "rcon_Address","");		
		Compilator_Include_Folders = inifile.ReadString("Compiler", "Include", Compilator_Include_Folders);
		rcon_Port = inifile.ReadInteger("Server", "rcon_port", rcon_Port);
		Compilator_Params = inifile.ReadString("Compiler", "Parameters","");		
		
		
		
		
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

		}*/
		Console.WriteLine(MapReload);
		#if DEBUG
		Debug.Print("MapReload\t\t=" + MapReload);
		Debug.Print("Compilator\t\t=" + Compilator);
		Debug.Print("Compilator_Folder\t=" + Compilator_Folder);
		Debug.Print("Compilator_Params\t=" + Compilator_Params);
		Debug.Print("Compilator_Include_Folders=" + Compilator_Include_Folders);
		Debug.Print("SRCDS_Folder=" + SRCDS_Folder);
		Console.WriteLine("SRCDS_Folder\t{0}",SRCDS_Folder);
		Debug.Print("rcon_address=" + rcon_Address);
		Debug.Print("rcon_port=" + rcon_Port);
		Debug.Print("rcon_password=" + rcon_password);
		#endif

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
	//*******************************************
	public static void CheckFolderString(ref string s)	{
	//*******************************************
		s=s.Trim();
		if (!s.EndsWith("\\")) s+="\\";
	}
	public static void CheckFolderString(ref string s,string basepath)
	{
		s=s.Trim();
		if (!s.EndsWith("\\")) s+="\\";
		if (!s.StartsWith("\\") & !s.StartsWith("\\\\") & s.Substring(1,2)!=":\\") s=basepath+s;
		
	}
	public static string FolderDifference(string Minuend  ,string Subtrahend)
	{
		if (Minuend.StartsWith(Subtrahend))
			return Minuend.Substring(Subtrahend.Length);
		else
			return Minuend;
	}
	private static void CopyDirectory(string sourcePath, string destinationPath)
	{
		//-----------------------------------------------------------------------
		System.IO.DirectoryInfo sourceDirectoryInfo = new System.IO.DirectoryInfo(sourcePath);

		// If the destination folder don't exist then create it
		if (!System.IO.Directory.Exists(destinationPath)) {
			System.IO.Directory.CreateDirectory(destinationPath);
		}

		System.IO.FileSystemInfo fileSystemInfo = null;
		foreach (FileSystemInfo fileSystemInfo_loopVariable in sourceDirectoryInfo.GetFileSystemInfos()) {
			fileSystemInfo = fileSystemInfo_loopVariable;
			string destinationFileName = System.IO.Path.Combine(destinationPath, fileSystemInfo.Name);

			// Now check whether its a file or a folder and take action accordingly
			if (fileSystemInfo is System.IO.FileInfo) {
				System.IO.File.Copy(fileSystemInfo.FullName, destinationFileName, true);Console.WriteLine(
					FolderDifference(sourcePath,PluginFolder)+"\\"+fileSystemInfo);
			} else {
				// Recursively call the mothod to copy all the neste folders
				CopyDirectory(fileSystemInfo.FullName, destinationFileName);
			}
		}
	}

	}
}