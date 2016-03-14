'-> If no strucured folders only compile and copy smx file
'-> Исключить из копированаия папку nppBackup
'-> Если нет ошибок, то закрыть окно через 5 секунд
'-> Добавить создание резерных копий версий перед компиляцией
'-> Добавить в копирование папки include ( Кроме include sourcemod)
Imports FileIO
Imports System
Imports System.IO

Module Program
	Dim Compilator As String = "spcomp.exe"
	Dim Compilator_Folder As String
	Dim Plugin_Author As String
	Dim rcon_Address As String = "127.0.0.1"
	Dim rcon_Port As Integer = 27015
	Dim rcon_password As String
	Dim ConfigFile As String 
	Dim SRCDS_Folder As String 
	Dim Compilator_Params As String ="vasym=""1"" -O2"
	Dim Compilator_Include_Folders As String  ="include"
	Dim mySMcomp_Folder As String
	Dim mySMcomp_Folders_Strucure As Integer = 2
	
	Dim TextChange As New ConsoleTextColor()
	Dim SourceFile As String
	Dim SourceFolder As String
	dim fso As Object
	Sub Main()		
		Console.Title = "My SourceMod Compiler Helper "+Globals.ScriptEngineBuildVersion.ToString	
		TextChange.TextColor(ConsoleTextColor.Foreground.Cyan + ConsoleTextColor.Foreground.Intensity)		
		Console.WriteLine("Compiler shell for SourceMod by Skorik 2013")
		Console.WriteLine("-------------------------------------------")
		TextChange.ResetColor()
		mySMcomp_Folder=My.Application.Info.DirectoryPath+"\" 'Gets the directory where the application is stored.		
		Debug.Print ("mySMcomp_Folder="&mySMcomp_Folder)
		
		Debug.Print ("args count="&My.Application.CommandLineArgs.Count)	
		'
		'Pars argumets
		'
		If My.Application.CommandLineArgs.Count<1 Then
			Console.WriteLine("Usage: MySMcompiler <file.sp>")
			ScriptFinish
			End
		End If
		Console.Title = Console.Title + " "+ My.Application.CommandLineArgs(0)+" "+Now
		SourceFile=My.Application.CommandLineArgs(0)
		Debug.Print ("SourceFile="&SourceFile)
		fso = CreateObject("Scripting.FileSystemObject")		'http://msdn.microsoft.com/en-us/library/6kxy1a51(v=vs.84).aspx
		
		If  Not fso.FileExists(SourceFile) Then
			Console.WriteLine("File " & SourceFile & " not found")
			ScriptFinish
			End
		End If
		SourceFolder =fso.GetParentFolderName(SourceFile)
		If Not fso.FolderExists(SourceFolder) Then 
			SourceFolder=Directory.GetCurrentDirectory()
		End If
		'SourceFolder =fso.GetFolder(SourceFolder).ShortPath +"\"
		SourceFolder =SourceFolder +"\"
		'Check folders strucure version
		Dim SMXFolder As String =fso.GetParentFolderName(SourceFolder)
		Dim tmpFolder=fso.GetBaseName(SMXFolder)
		If UCase(tmpFolder)="SCRIPTING" Then
			mySMcomp_Folders_Strucure=2
			SMXFolder=fso.GetParentFolderName(SMXFolder)+"\plugins"
			If Not fso.FolderExists(SMXFolder) Then 
				fso.CreateFolder(SMXFolder)
			End If
			SMXFolder=fso.GetFolder(SMXFolder).ShortPath+"\"
		Else
			mySMcomp_Folders_Strucure=1
			SMXFolder=SourceFolder
			Console.WriteLine("INF: No Folder structure")
		End If				
		SourceFile=fso.GetBaseName(fso.GetFile(SourceFile))
		'
		'Read config
		'
		
		Call GetConfigFile				
		'---
		Dim Compilator_Include_Folder As String()  =Compilator_Include_Folders.Split(";")		
		Compilator_Include_Folders=""
		For Each s As String In  Compilator_Include_Folder
            If Not String.IsNullOrWhiteSpace(s) Then
            	Compilator_Include_Folders=Compilator_Include_Folders+ _
            		" -i"+Compilator_Folder+s.Trim()
            End If 
		Next s
        '
        'Create include file
        '
		Dim  f_inc=fso.CreateTextFile(SourceFolder+"datetimecomp.inc",true)
		f_inc.WriteLine("#if defined DEBUG")
		f_inc.WriteLine("	#define PLUGIN_DATETIME """+cstr(Now)+"""")
		f_inc.WriteLine("	#if defined PLUGIN_VERSION")
		f_inc.WriteLine("		#undef PLUGIN_VERSION")
		f_inc.WriteLine("	#endif")
		f_inc.WriteLine("	#define PLUGIN_VERSION """+CStr(Now)+"""")
		f_inc.WriteLine("#endif")
		f_inc.WriteLine("#if !defined PLUGIN_NAME")
		f_inc.WriteLine("	#define PLUGIN_NAME """+SourceFile+"""")		
		f_inc.WriteLine("#endif")
		f_inc.WriteLine("#if !defined PLUGIN_AUTHOR")
		f_inc.WriteLine("	#define PLUGIN_AUTHOR """+Plugin_Author+"""")
		f_inc.WriteLine("#endif")	

		f_inc.close

		If  fso.FileExists(SourceFolder & SourceFile & ".err") Then
			fso.DeleteFile (SourceFolder & SourceFile & ".err",True)
		End If
		If  fso.FileExists(SMXFolder & SourceFile & ".smx") Then
			fso.DeleteFile (SMXFolder & SourceFile & ".smx",True)
		End If
		'
		'Compiling
		'
		'
'SourcePawn Compiler 1.4.7-dev
'Copyright (c) 1997-2006, ITB CompuPhase, (C)2004-2008 AlliedModders, LLC
'
'Usage:   spcomp <filename> [filename...] [options]
'
'Options:
'         -A<num>  alignment in bytes of the data segment and the stack
'         -a       output assembler code
'         -c<name> codepage name or number; e.g. 1252 for Windows Latin-1
'         -Dpath   active directory path
'         -e<name> set name of error file (quiet compile)
'         -H<hwnd> window handle to send a notification message on finish
'         -i<name> path for include files
'         -l       create list file (preprocess only)
'         -o<name> set base name of (P-code) output file
'         -O<num>  optimization level (default=-O2)
'             0    no optimization
'             2    full optimizations
'         -p<name> set name of "prefix" file
'         -r[name] write cross reference report to console or to specified file
'         -S<num>  stack/heap size in cells (default=4096)
'         -s<num>  skip lines from the input file
'         -t<num>  TAB indent size (in character positions, default=8)
'         -v<num>  verbosity level; 0=quiet, 1=normal, 2=verbose (default=1)
'         -w<num>  disable a specific warning by its number
'         -X<num>  abstract machine size limit in bytes
'         -XD<num> abstract machine data/stack size limit in bytes
'         -\       use '\' for escape characters
'         -^       use '^' for escape characters
'         -;[+/-]  require a semicolon to end each statement (default=-)
'         sym=val  define constant "sym" with value "val"
'         sym=     define constant "sym" with value 0
'
'Options may start with a dash or a slash; the options "-d0" and "/d0" are equivalent.
'
'Options with a value may optionally separate the value from the option letter
'with a colon (":") or an equal sign ("="). That is, the options "-d0", "-d=0"
'and "-d:0" are all equivalent.
		If  Not fso.FileExists(Compilator_Folder+Compilator) Then
			Console.WriteLine("ERR: File compiler" & Compilator_Folder+Compilator & " not found")
			ScriptFinish (true)
			End
		End If
		TextChange.TextColor(ConsoleTextColor.Foreground.White+ConsoleTextColor.Foreground.Intensity )
		Console.WriteLine("Compiling")
		TextChange.ResetColor()
		'http://msdn.microsoft.com/en-us/library/system.diagnostics.processstartinfo.useshellexecute.aspx
		Dim compiler As New Process()
		compiler.StartInfo.FileName = Compilator_Folder+Compilator
		compiler.StartInfo.Arguments =SourceFolder & SourceFile+".sp "+Compilator_Params+ _
			" -e"+SourceFile+".err"+ _
			" -D"+SourceFolder+ _
			" -o"+SMXFolder+SourceFile+ _
			" -w213"+ _
			Compilator_Include_Folders
		TextChange.TextColor(ConsoleTextColor.Foreground.Green+ConsoleTextColor.Foreground.Intensity )
		Console.WriteLine(compiler.StartInfo.FileName+" "+compiler.StartInfo.Arguments)		
		TextChange.ResetColor()		
		
		compiler.StartInfo.UseShellExecute = False
		compiler.StartInfo.RedirectStandardOutput = True
		compiler.Start()		
		Console.WriteLine(compiler.StandardOutput.ReadToEnd())		
		compiler.WaitForExit()
		'http://msdn.microsoft.com/en-us/library/system.diagnostics.process.exitcode.aspx
		If compiler.ExitCode>0 Then
			Const ForReading = 1
			Dim errFile = (fso.GetFile(SourceFolder+SourceFile+".err")).OpenAsTextStream(ForReading)
			Console.WriteLine(errFile.ReadAll)
			ScriptFinish (true)
			End
		End If
		'
		' Copy to server
		'
		If Not fso.FolderExists(SRCDS_Folder) Then 
			Console.WriteLine("ERR:Folder for copy smx file "+SRCDS_Folder+"not found")
			ScriptFinish 
			End
		End If
		
		If mySMcomp_Folders_Strucure=2 Then
			SMXFolder=fso.GetParentFolderName(SMXFolder)
			SMXFolder=fso.GetParentFolderName(SMXFolder)
			SMXFolder=fso.GetParentFolderName(SMXFolder)
			Console.WriteLine("INF: Copy folder "+SMXFolder+" to "+SRCDS_Folder)
			CopyDirectory(SMXFolder,SRCDS_Folder)			
		ElseIf mySMcomp_Folders_Strucure=1
			SRCDS_Folder=LCase(SRCDS_Folder)&"addons\sourcemod\plugins\"
			If Not fso.FolderExists(SRCDS_Folder) Then 
				Console.WriteLine("ERR:Folder for copy smx file "+SRCDS_Folder+"not found")
				ScriptFinish
				End
			End if	
			Console.WriteLine("INF: Copy file "+SMXFolder+SourceFile+".smx"+" to "+SRCDS_Folder)
			If  fso.FileExists(SRCDS_Folder & SourceFile & ".smx") Then
				fso.DeleteFile (SRCDS_Folder & SourceFile & ".smx",True)
			End If
			fso.CopyFile(SMXFolder+SourceFile+".smx", SRCDS_Folder, True)
			If  Not fso.FileExists(SRCDS_Folder & SourceFile & ".smx") Then
				Console.WriteLine("ERR:File "+SMXFolder+SourceFile+".smx not copied")
				ScriptFinish
			End If
		End If
		'
		' Restart plugin 
		'
		Dim m_ip As Object = nothing
		Try
        	m_ip = Net.IPAddress.Parse(rcon_address)
            Catch ex As Exception
                Try
                    Dim a_IP() As Net.IPAddress
                    a_IP = Net.Dns.GetHostAddresses(rcon_address)
                    m_ip = a_IP(0)
                Catch ex_inner As Exception
                	'Throw New Exception("Invalid IP address or DNS name.", ex_inner)
                	'Console.Write("ERR:Invalid IP address or DNS name "+rcon_address)
                	Console.Write("ERR: On resolve Hostname or IP "+rcon_address+" follow error was occurred."+ex_inner.Message)
                	ScriptFinish
                	end
                End Try
		End Try
		Console.WriteLine("INF: Server IP "+m_ip.ToString+":"+rcon_Port.ToString)
		'-> Test connection to server
		Try 
		Dim srcds As New RCON(m_ip,rcon_port)		
		srcds.SetPassword(rcon_password)
		If Not srcds.Auth() Then
			Console.WriteLine("Incorrect RCON  password server")
			ScriptFinish
		End If
		Console.WriteLine(srcds.SendCommand("sm plugins unload "+SourceFile))
		Console.WriteLine(srcds.SendCommand("sm plugins load "+SourceFile))
		Catch ex As Exception
			Console.WriteLine("ERR: On connect to srcds follow error was occurred."+ex.Message)
		End Try	
		
		Console.WriteLine("Finish")
		
		
		ScriptFinish (false)
		
		
	End Sub
	'-----------------------------------------------------------------------
	Sub GetConfigFile		
		Const myConfigFile As String = "MySMcompile.ini"
		'Find ini:
		' 1. Folder with sp file
		' 2. Folder with mysmcomp.exe		
		ConfigFile=SourceFolder+myConfigFile
		
		If  Not fso.FileExists(ConfigFile) Then
			ConfigFile= mySMcomp_Folder +"\"+myConfigFile
			If  Not fso.FileExists(ConfigFile) Then
				Console.WriteLine("WARN: File "+myConfigFile+" not found. Use default value")	
				Console.ReadKey(True)
				ConfigFile=""
				
				Compilator_Folder=mySMcomp_Folder
			End If			
		End If
		If Not  ConfigFile.IsNullOrEmpty(ConfigFile) Then
			'http://msdn.microsoft.com/en-us/library/system.string.isnullorempty.aspx
			Console.WriteLine("INF: Use ini file "+ConfigFile)
			
			Dim inifile As New Inifile(ConfigFile)
			Compilator			=inifile.LoadString("Compiler", "Compilator", Compilator)
			Compilator_Folder	=inifile.LoadString("Compiler", "Compilator_Folder", mySMcomp_Folder)
			Compilator_Params	=inifile.LoadString("Compiler", "Parameters", Compilator_Params)
			Compilator_Include_Folders=inifile.LoadString("Compiler", "Include", Compilator_Include_Folders)
			SRCDS_Folder		=inifile.LoadString("Server", "SRCDS_Folder")
			rcon_address		=inifile.LoadString("Server", "rcon_address",rcon_address)
			rcon_port			=inifile.LoadInteger("Server", "rcon_port",rcon_port)
			rcon_password		=inifile.LoadString("Server", "rcon_password",rcon_password)
			Plugin_Author		=inifile.LoadString("Compiler", "Plugin_Author","")
			 
		end if
		Debug.Print ("Compilator		="&Compilator)
		Debug.Print ("Compilator_Folder	="&Compilator_Folder)
		Debug.Print ("Compilator_Params	="&Compilator_Params)
		Debug.Print ("Compilator_Include_Folders="&Compilator_Include_Folders)
		Debug.Print ("SRCDS_Folder="&SRCDS_Folder)
		Debug.Print ("rcon_address="&rcon_address)
		Debug.Print ("rcon_port="&rcon_port)
		Debug.Print ("rcon_password="&rcon_password)
		
'[Compiler]
'Compilator="spcomp.exe"
'Compilator_Folder="c:\pro\SourceMod\"
'Parameters=
'Include=
'
'[Server]
'rcon_address="deploy2"
'rcon_port=27015
'rcon_password="PI"
'SRCDS_Folder="\\deploy2\c$\Program Files\srcds.css.75\css\cstrike\" 
'SRCDS_FTP=
		
End Sub
'-----------------------------------------------------------------------
Sub ScriptFinish (Optional pause As Boolean = False)
If pause Then 
	Console.Writeline
	Console.Write("Press any key to continue . . . ")
	Console.ReadKey(True)
End if
End Sub
'-----------------------------------------------------------------------
Private Sub CopyDirectory(ByVal sourcePath As String, ByVal destinationPath As String)
'-----------------------------------------------------------------------
Dim sourceDirectoryInfo As New System.IO.DirectoryInfo(sourcePath)

    ' If the destination folder don't exist then create it
    If Not System.IO.Directory.Exists(destinationPath) Then
        System.IO.Directory.CreateDirectory(destinationPath)
    End If

    Dim fileSystemInfo As System.IO.FileSystemInfo
    For Each fileSystemInfo In sourceDirectoryInfo.GetFileSystemInfos
        Dim destinationFileName As String =
            System.IO.Path.Combine(destinationPath, fileSystemInfo.Name)

        ' Now check whether its a file or a folder and take action accordingly
        If TypeOf fileSystemInfo Is System.IO.FileInfo Then
            System.IO.File.Copy(fileSystemInfo.FullName, destinationFileName, True)
        Else
            ' Recursively call the mothod to copy all the neste folders
            CopyDirectory(fileSystemInfo.FullName, destinationFileName)
        End If
    Next
	End Sub
End Module
