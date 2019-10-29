/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using DOL.GS.PacketHandler;
using DOL.Language;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DOL.GS.Commands
{
	[Cmd(
		"&code",
		ePrivLevel.Admin,
		"Commands.Admin.Code.Description",
		"Commands.Admin.Code.Usage")]
	public class DynCodeCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public async static void ExecuteCode(GameClient client, string code)
		{
			StringBuilder text = new StringBuilder();
			text.Append("using System;\n");
			text.Append("using System.Reflection;\n");
			text.Append("using System.Collections;\n");
			text.Append("using System.Threading;\n");
			text.Append("using DOL;\n");
			text.Append("using DOL.AI;\n");
			text.Append("using DOL.AI.Brain;\n");
			text.Append("using DOL.Database;\n");
			text.Append("using DOL.GS;\n");
			text.Append("using DOL.GS.Movement;\n");
			text.Append("using DOL.GS.Housing;\n");
			text.Append("using DOL.GS.Keeps;\n");
			text.Append("using DOL.GS.Quests;\n");
			text.Append("using DOL.GS.Commands;\n");
			text.Append("using DOL.GS.Scripts;\n");
			text.Append("using DOL.GS.PacketHandler;\n");
			text.Append("using DOL.Events;\n");
			text.Append("using log4net;\n");
			text.Append(@"
Action<GameObject, GamePlayer> test = (target, player) =>
	{
		var Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		var Client = player?.Client;
		var targetNpc = target as GameNPC;
		Action<object> print = obj =>
		{
			string str = (obj == null) ? ""(null)"" : obj.ToString();
			if (Client == null || Client.Player == null)
				Log.Debug(str);
			else
				Client.Out.SendMessage(str, eChatType.CT_System, eChatLoc.CL_SystemWindow);
		};
");
			text.Append(code);
			text.Append(@";
	};
return test;
");

			ScriptOptions options = ScriptOptions.Default.AddReferences(AppDomain.CurrentDomain.GetAssemblies());
			var resultObj = await CSharpScript.EvaluateAsync(text.ToString(), options);
			var result = resultObj as Action<GameObject, GamePlayer>;

			try
			{
				if (result != null)
				{
					result(client?.Player?.TargetObject, client?.Player);
				}

				if (client.Player != null)
				{
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Admin.Code.CodeExecuted"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				else
				{
					log.Debug("Code Executed.");
				}

			}
			catch (Exception ex)
			{
				if (client.Player != null)
				{
					string[] errors = ex.ToString().Split('\n');
					foreach (string error in errors)
						client.Out.SendMessage(error, eChatType.CT_System, eChatLoc.CL_PopupWindow);
				}
				else
				{
					log.Debug("Error during execution.");
				}
			}
		}


		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length == 1)
			{
				DisplaySyntax(client);
				return;
			}
			string code = String.Join(" ", args, 1, args.Length - 1);
			ExecuteCode(client, code);
		}
	}
}
