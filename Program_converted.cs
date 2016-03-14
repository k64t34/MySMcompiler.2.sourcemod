using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
//-> If no strucured folders only compile and copy smx file
//-> Исключить из копированаия папку nppBackup
//-> Если нет ошибок, то закрыть окно через 5 секунд
//-> Добавить создание резерных копий версий перед компиляцией
//-> Добавить в копирование папки include ( Кроме include sourcemod)
using FileIO;
using System.IO;

static class Program
{
	static string Compilator = "spcomp.exe";
	static string Compilator_Folder;
	static string Plugin_Author;
	static string rcon_Address = "127.0.0.1";
	static int rcon_Port = 27015;
	static string rcon_password;
	static string ConfigFile;
	static string SRCDS_Folder;
	static string Compilator_Params = "vasym=\"1\" -O2";
	static string Compilator_Include_Folders = "include";
	static string mySMcomp_Folder;
	static int mySMcomp_Folders_Strucure = 2;

	static ConsoleTextColor TextChange = new ConsoleTextColor();
	static string SourceFile;
	static string SourceFolder;
	static object fso;
	public static void Main()
	{
		Console.Title = "My SourceMod Compiler Helper " + Globals.ScriptEngineBuildVersion.ToString();
		TextChange.TextColor(ConsoleTextColor.Foreground.Cyan + ConsoleTextColor.Foreground.Intensity);
		Console.WriteLine("Compiler shell for SourceMod by Skorik 2013");
		Console.WriteLine("-------------------------------------------");
		TextChange.ResetColor();
		mySMcomp_Folder = MySMcompile.My.MyProject.Application.Info.DirectoryPath + "\\";
		//Gets the directory where the application is stored.		
		Debug.Print("mySMcomp_Folder=" + mySMcomp_Folder);

		Debug.Print("args count=" + MySMcompile.My.MyProject.Application.CommandLineArgs.Count);
		//
		//Pars argumets
		//
		if (MySMcompile.My.MyProject.Application.CommandLineArgs.Count < 1) {
			Console.WriteLine("Usage: MySMcompiler <file.sp>");
			ScriptFinish();
			System.Environment.Exit(0);
		}
		Console.Title = Console.Title + " " + MySMcompile.My.MyProject.Application.CommandLineArgs[0] + " " + DateAndTime.Now;
		SourceFile = MySMcompile.My.MyProject.Application.CommandLineArgs[0];
		Debug.Print("SourceFile=" + SourceFile);
		fso = Interaction.CreateObject("Scripting.FileSystemObject");
		//http://msdn.microsoft.com/en-us/library/6kxy1a51(v=vs.84).aspx

		if (!fso.FileExists(SourceFile)) {
			Console.WriteLine("File " + SourceFile + " not found");
			ScriptFinish();
			System.Environment.Exit(0);
		}
		SourceFolder = fso.GetParentFolderName(SourceFile);
		if (!fso.FolderExists(SourceFolder)) {
			SourceFolder = Directory.GetCurrentDirectory();
		}
		//SourceFolder =fso.GetFolder(SourceFolder).ShortPath +"\"
		SourceFolder = SourceFolder + "\\";
		//Check folders strucure version
		string SMXFolder = fso.GetParentFolderName(SourceFolder);
		dynamic tmpFolder = fso.GetBaseName(SMXFolder);
		if (Strings.UCase(tmpFolder) == "SCRIPTING") {
			mySMcomp_Folders_Strucure = 2;
			SMXFolder = fso.GetParentFolderName(SMXFolder) + "\\plugins";
			if (!fso.FolderExists(SMXFolder)) {
				fso.CreateFolder(SMXFolder);
			}
			SMXFolder = fso.GetFolder(SMXFolder).ShortPath + "\\";
		} else {
			mySMcomp_Folders_Strucure = 1;
			SMXFolder = SourceFolder;
			Console.WriteLine("INF: No Folder structure");
		}
		SourceFile = fso.GetBaseName(fso.GetFile(SourceFile));
		//
		//Read config
		//

		GetConfigFile();
		//---
		string[] Compilator_Include_Folder = Compilator_Include_Folders.Split(";");
		Compilator_Include_Folders = "";
		foreach (string s in Compilator_Include_Folder) {
			if (!string.IsNullOrWhiteSpace(s)) {
				Compilator_Include_Folders = Compilator_Include_Folders + " -i" + Compilator_Folder + s.Trim();
			}
		}
		//
		//Create include file
		//
		dynamic f_inc = fso.CreateTextFile(SourceFolder + "datetimecomp.inc", true);
		f_inc.WriteLine("#if defined DEBUG");
		f_inc.WriteLine("\t#define PLUGIN_DATETIME \"" + Convert.ToString(DateAndTime.Now) + "\"");
		f_inc.WriteLine("\t#if defined PLUGIN_VERSION");
		f_inc.WriteLine("\t\t#undef PLUGIN_VERSION");
		f_inc.WriteLine("\t#endif");
		f_inc.WriteLine("\t#define PLUGIN_VERSION \"" + Convert.ToString(DateAndTime.Now) + "\"");
		f_inc.WriteLine("#endif");
		f_inc.WriteLine("#if !defined PLUGIN_NAME");
		f_inc.WriteLine("\t#define PLUGIN_NAME \"" + SourceFile + "\"");
		f_inc.WriteLine("#endif");
		f_inc.WriteLine("#if !defined PLUGIN_AUTHOR");
		f_inc.WriteLine("\t#define PLUGIN_AUTHOR \"" + Plugin_Author + "\"");
		f_inc.WriteLine("#endif");

		f_inc.close();

		if (fso.FileExists(SourceFolder + SourceFile + ".err")) {
			fso.DeleteFile(SourceFolder + SourceFile + ".err", true);
		}
		if (fso.FileExists(SMXFolder + SourceFile + ".smx")) {
			fso.DeleteFile(SMXFolder + SourceFile + ".smx", true);
		}
		//
		//Compiling
		//
		//
		//SourcePawn Compiler 1.4.7-dev
		//Copyright (c) 1997-2006, ITB CompuPhase, (C)2004-2008 AlliedModders, LLC
		//
		//Usage:   spcomp <filename> [filename...] [options]
		//
		//Options:
		//         -A<num>  alignment in bytes of the data segment and the stack
		//         -a       output assembler code
		//         -c<name> codepage name or number; e.g. 1252 for Windows Latin-1
		//         -Dpath   active directory path
		//         -e<name> set name of error file (quiet compile)
		//         -H<hwnd> window handle to send a notification message on finish
		//         -i<name> path for include files
		//         -l       create list file (preprocess only)
		//         -o<name> set base name of (P-code) output file
		//         -O<num>  optimization level (default=-O2)
		//             0    no optimization
		//             2    full optimizations
		//         -p<name> set name of "prefix" file
		//         -r[name] write cross reference report to console or to specified file
		//         -S<num>  stack/heap size in cells (default=4096)
		//         -s<num>  skip lines from the input file
		//         -t<num>  TAB indent size (in character positions, default=8)
		//         -v<num>  verbosity level; 0=quiet, 1=normal, 2=verbose (default=1)
		//         -w<num>  disable a specific warning by its number
		//         -X<num>  abstract machine size limit in bytes
		//         -XD<num> abstract machine data/stack size limit in bytes
		//         -\       use '\' for escape characters
		//         -^       use '^' for escape characters
		//         -;[+/-]  require a semicolon to end each statement (default=-)
		//         sym=val  define constant "sym" with value "val"
		//         sym=     define constant "sym" with value 0
		//
		//Options may start with a dash or a slash; the options "-d0" and "/d0" are equivalent.
		//
		//Options with a value may optionally separate the value from the option letter
		//with a colon (":") or an equal sign ("="). That is, the options "-d0", "-d=0"
		//and "-d:0" are all equivalent.
		if (!fso.FileExists(Compilator_Folder + Compilator)) {
			Console.WriteLine("ERR: File compiler" + Compilator_Folder + Compilator + " not found");
			ScriptFinish(true);
			System.Environment.Exit(0);
		}
		TextChange.TextColor(ConsoleTextColor.Foreground.White + ConsoleTextColor.Foreground.Intensity);
		Console.WriteLine("Compiling");
		TextChange.ResetColor();
		//http://msdn.microsoft.com/en-us/library/system.diagnostics.processstartinfo.useshellexecute.aspx
		Process compiler = new Process();
		compiler.StartInfo.FileName = Compilator_Folder + Compilator;
		compiler.StartInfo.Arguments = SourceFolder + SourceFile + ".sp " + Compilator_Params + " -e" + SourceFile + ".err" + " -D" + SourceFolder + " -o" + SMXFolder + SourceFile + " -w213" + Compilator_Include_Folders;
		TextChange.TextColor(ConsoleTextColor.Foreground.Green + ConsoleTextColor.Foreground.Intensity);
		Console.WriteLine(compiler.StartInfo.FileName + " " + compiler.StartInfo.Arguments);
		TextChange.ResetColor();

		compiler.StartInfo.UseShellExecute = false;
		compiler.StartInfo.RedirectStandardOutput = true;
		compiler.Start();
		Console.WriteLine(compiler.StandardOutput.ReadToEnd());
		compiler.WaitForExit();
		//http://msdn.microsoft.com/en-us/library/system.diagnostics.process.exitcode.aspx
		if (compiler.ExitCode > 0) {
			const dynamic ForReading = 1;
			dynamic errFile = (fso.GetFile(SourceFolder + SourceFile + ".err")).OpenAsTextStream(ForReading);
			Console.WriteLine(errFile.ReadAll);
			ScriptFinish(true);
			System.Environment.Exit(0);
		}
		//
		// Copy to server
		//
		if (!fso.FolderExists(SRCDS_Folder)) {
			Console.WriteLine("ERR:Folder for copy smx file " + SRCDS_Folder + "not found");
			ScriptFinish();
			System.Environment.Exit(0);
		}

		if (mySMcomp_Folders_Strucure == 2) {
			SMXFolder = fso.GetParentFolderName(SMXFolder);
			SMXFolder = fso.GetParentFolderName(SMXFolder);
			SMXFolder = fso.GetParentFolderName(SMXFolder);
			Console.WriteLine("INF: Copy folder " + SMXFolder + " to " + SRCDS_Folder);
			CopyDirectory(SMXFolder, SRCDS_Folder);
		} else if (mySMcomp_Folders_Strucure == 1) {
			SRCDS_Folder = Strings.LCase(SRCDS_Folder) + "addons\\sourcemod\\plugins\\";
			if (!fso.FolderExists(SRCDS_Folder)) {
				Console.WriteLine("ERR:Folder for copy smx file " + SRCDS_Folder + "not found");
				ScriptFinish();
				System.Environment.Exit(0);
			}
			Console.WriteLine("INF: Copy file " + SMXFolder + SourceFile + ".smx" + " to " + SRCDS_Folder);
			if (fso.FileExists(SRCDS_Folder + SourceFile + ".smx")) {
				fso.DeleteFile(SRCDS_Folder + SourceFile + ".smx", true);
			}
			fso.CopyFile(SMXFolder + SourceFile + ".smx", SRCDS_Folder, true);
			if (!fso.FileExists(SRCDS_Folder + SourceFile + ".smx")) {
				Console.WriteLine("ERR:File " + SMXFolder + SourceFile + ".smx not copied");
				ScriptFinish();
			}
		}
		//
		// Restart plugin 
		//
		object m_ip = null;
		try {
			m_ip = System.Net.IPAddress.Parse(rcon_Address);
		} catch (Exception ex) {
			try {
				System.Net.IPAddress[] a_IP = null;
				a_IP = System.Net.Dns.GetHostAddresses(rcon_Address);
				m_ip = a_IP[0];
			} catch (Exception ex_inner) {
				//Throw New Exception("Invalid IP address or DNS name.", ex_inner)
				//Console.Write("ERR:Invalid IP address or DNS name "+rcon_address)
				Console.Write("ERR: On resolve Hostname or IP " + rcon_Address + " follow error was occurred." + ex_inner.Message);
				ScriptFinish();
				System.Environment.Exit(0);
			}
		}
		Console.WriteLine("INF: Server IP " + m_ip.ToString() + ":" + rcon_Port.ToString());
		//-> Test connection to server
		try {
			RCON srcds = new RCON(m_ip, rcon_Port);
			srcds.SetPassword(rcon_password);
			if (!srcds.Auth()) {
				Console.WriteLine("Incorrect RCON  password server");
				ScriptFinish();
			}
			Console.WriteLine(srcds.SendCommand("sm plugins unload " + SourceFile));
			Console.WriteLine(srcds.SendCommand("sm plugins load " + SourceFile));
		} catch (Exception ex) {
			Console.WriteLine("ERR: On connect to srcds follow error was occurred." + ex.Message);
		}

		Console.WriteLine("Finish");


		ScriptFinish(false);


	}
//-----------------------------------------------------------------------
	public static void GetConfigFile()
	{
		const string myConfigFile = "MySMcompile.ini";
		//Find ini:
		// 1. Folder with sp file
		// 2. Folder with mysmcomp.exe		
		ConfigFile = SourceFolder + myConfigFile;

		if (!fso.FileExists(ConfigFile)) {
			ConfigFile = mySMcomp_Folder + "\\" + myConfigFile;
			if (!fso.FileExists(ConfigFile)) {
				Console.WriteLine("WARN: File " + myConfigFile + " not found. Use default value");
				Console.ReadKey(true);
				ConfigFile = "";

				Compilator_Folder = mySMcomp_Folder;
			}
		}
		if (!ConfigFile.IsNullOrEmpty(ConfigFile)) {
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
		Debug.Print("rcon_password=" + rcon_password);

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
	//-----------------------------------------------------------------------
	public static void ScriptFinish(bool pause = false)
	{
		if (pause) {
			Console.WriteLine();
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	}
	//-----------------------------------------------------------------------
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
				System.IO.File.Copy(fileSystemInfo.FullName, destinationFileName, true);
			} else {
				// Recursively call the mothod to copy all the neste folders
				CopyDirectory(fileSystemInfo.FullName, destinationFileName);
			}
		}
	}
}
