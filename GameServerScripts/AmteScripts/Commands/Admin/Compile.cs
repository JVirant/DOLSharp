using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AmteScripts.Utils;
using DOL.Events;
using DOL.GS.PacketHandler;
using Microsoft.CSharp;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&compile",
		ePrivLevel.Admin,
		"Compile un script",
		"/compile <path> Compile un script")]
	public class CompileCommandHandler : AbstractCommandHandler, ICommandHandler
	{
        public void OnCommand(GameClient client, string[] args)
        {
			if (args.Length == 1)
			{
				DisplaySyntax(client);
				return;
			}
			ExecuteCode(client, string.Join(" ", args, 1, args.Length - 1));
        }

		private static readonly List<Assembly> assemblies = new List<Assembly>();
		public static bool ExecuteCode(GameClient client, string code)
		{
			var cc = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
			var cp = new CompilerParameters();
			var files = new List<string>();

			var isDir = ((File.GetAttributes(code) & FileAttributes.Directory) == FileAttributes.Directory);
			if (isDir)
			{
				FunctionnalHelpers.Y<string>(
					f => path =>
					     {
					     	Directory.GetDirectories(path).Foreach(f);
					     	Directory.GetFiles(path).Foreach(files.Add);
					     })(code);
			}

			GameServer.Instance.Configuration.ScriptAssemblies.Foreach(s => cp.ReferencedAssemblies.Add(s));
			cp.ReferencedAssemblies.Add("GameServerScripts.dll"); // Ah oui, il faut référencer les scripts
			cp.ReferencedAssemblies.Add("System.Core.dll");
			cp.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
			cp.CompilerOptions = @"/lib:." + Path.DirectorySeparatorChar + "lib /debug";
			cp.GenerateExecutable = false;
			cp.GenerateInMemory = true;
			cp.WarningLevel = 2;


			var cr = isDir ? cc.CompileAssemblyFromFile(cp, files.ToArray()) : cc.CompileAssemblyFromFile(cp, code);

			if (cr.Errors.HasErrors)
			{
				client.Out.SendMessage("Error Compiling Expression: ", eChatType.CT_System, eChatLoc.CL_PopupWindow);

				foreach (CompilerError err in cr.Errors)
					client.Out.SendMessage("   " + err.FileName + " Line:" + err.Line + " Col:" + err.Column + " : " + err.ErrorText,
					                       eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}

			var newAssembly = cr.CompiledAssembly;


			client.Out.SendMessage("Compilation réussie.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

			assemblies.Add(newAssembly);
			GC.Collect();
			return true;
		}
	}
}