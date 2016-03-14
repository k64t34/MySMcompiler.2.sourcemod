//EXT1 - Изменить проверку файла, если указан не полный путь к файлу а тольк имя
//EXT2 - Включить в ScriptFinish сообщение и код возврата
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
		const string Compilator="spcomp.exe";
		static string mySMcomp_Folder; //Gets the directory where the application is stored.	
		static string SourceFile; //Source programm file .sp
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
				Console.WriteLine("Usage: MySMcompiler <file.sp>");
				ScriptFinish(true);
				System.Environment.Exit(0);
			}			
			Console.Title = Console.Title + " " + args[0] + " ";// + DateAndTime.Now;
			SourceFile = args[0];			
			Debug.Print("SourceFile=" + SourceFile);
			
			//EXT1
			if (!File.Exists(SourceFile))
			{
				Console.WriteLine("File "+SourceFile+" not found");
				ScriptFinish(true);
				System.Environment.Exit(1);
			}
			
			
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	//****************************************************	
	public static void ScriptFinish(bool pause)
	//****************************************************		
	{
		if (pause) {
			Console.WriteLine();
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	}
	}
}