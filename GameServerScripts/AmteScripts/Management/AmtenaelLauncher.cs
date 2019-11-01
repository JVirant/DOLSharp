using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler.Client.v168;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.GS.Scripts
{
	public static class AmtenaelLauncher
	{
		#region Starting
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[ScriptLoadedEvent]
		public static void OnScriptsCompiled(DOLEvent e, object sender, EventArgs args)
		{
			_running = true;
			_listener = new TcpListener(IPAddress.Any, 10301);
			_listener.Start();
			_worker = new Thread(_Work);
			_worker.Start();
			if (log.IsInfoEnabled)
				log.Info("AmtenaelLauncher initialized...");
		}

		[ScriptUnloadedEvent]
		public static void OnScriptsUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			_running = false;
			try { _listener.Stop(); } catch { }
		}
		#endregion

		private static TcpListener _listener;
		private static Thread _worker;
		private static bool _running;

		private static void _Work()
		{
			while (_running)
			{
				try
				{
					var cl = _listener.AcceptTcpClient();
					var buffer = new byte[2048];
					cl.GetStream().BeginRead(buffer, 0, buffer.Length, _ReadingCallback, new KeyValuePair<TcpClient, byte[]>(cl, buffer));
				}
				catch (Exception e)
				{
					log.Error("[AmtenaelLauncher]", e);
				}
			}
		}

		private static Regex _loginValidation = new Regex("^[a-zA-Z0-9]*$");
		private static async void _ReadingCallback(IAsyncResult ar)
		{
			TcpClient client;
			byte[] buffer;
			try
			{
				var kv = (KeyValuePair<TcpClient, byte[]>)ar.AsyncState;
				client = kv.Key;
				buffer = kv.Value;
			}
			catch (Exception e)
			{
				log.Error("[AmtenaelLauncher]", e);
				return;
			}
			try
			{
				int size = client.GetStream().EndRead(ar);
				if (size >= 4)
				{
					var lines = Encoding.UTF8.GetString(buffer, 0, size).Split('\n');
					var login = lines[0];
					var password = lines[1];
					var uuid = lines[2];

					var resp = _GetToken(client, uuid, login, password);
					var respBuf = Encoding.UTF8.GetBytes(resp);
					await client.GetStream().WriteAsync(respBuf, 0, respBuf.Length);
				}
			}
			catch (Exception e)
			{
				log.Error("[AmtenaelLauncher]", e);
			}
			finally
			{
				try
				{
					client.Close();
				}
				catch (Exception e)
				{
					log.Error("[AmtenaelLauncher] Close client", e);
				}
			}
		}

		private static string _GetToken(TcpClient client, string uuid, string login, string password)
		{
			if (!_loginValidation.IsMatch(login))
				return "error:Le nom du compte n'est pas valide.";

			var account = _findOrCreateAccount(client, login, password);
			if (account == null)
				return "error:Mot de passe incorrect.";

			if (_IsBan(account))
				return "error:Ce compte a été banni.";

			if (_IsConnected(account))
				return "error:Vous êtes déjà connecté.";

			var buffer = new byte[8];
			RandomNumberGenerator.Create().GetBytes(buffer);
			var token = Convert.ToBase64String(buffer);

			lock (LoginRequestHandler.Token2AccountSync)
				LoginRequestHandler.Token2Account.Add(token, (account.Name, uuid));
			var charlist = "";
			if (account.Characters != null)
				charlist = account.Characters.Aggregate("", (list, ch) => $"{ch.Name} {ch.Realm}\n{list}");
			return $"{token}\n{charlist}";
		}

		private static bool _IsBan(Account account)
		{
			return false;
		}

		private static bool _IsConnected(Account account)
		{
			return false;
		}

		private static DateTime _lastCleanup = DateTime.Now;
		private static Dictionary<string, int> _attemptIps = new Dictionary<string, int>();

		private static Account _findOrCreateAccount(TcpClient client, string name, string password)
		{
			var ip = client.Client.RemoteEndPoint.ToString();
			int count;
			if (_attemptIps.TryGetValue(ip, out count) && count > 20)
				return null;
			if (_lastCleanup.Ticks + 60*1000*10000 < DateTime.Now.Ticks)
			{
				_attemptIps.Clear();
				_lastCleanup = DateTime.Now;
			}

			var hashedPassword = LoginRequestHandler.CryptPassword(password);
			var account = GameServer.Database.FindObjectByKey<Account>(name.ToLower());

			if (account != null)
			{
				// check password
				if (!account.Password.StartsWith("##"))
				{
					account.Password = LoginRequestHandler.CryptPassword(account.Password);
				}

				if (!hashedPassword.Equals(account.Password))
				{
					log.Info($"({client.Client.RemoteEndPoint}) {account.Name}: Wrong password!");
					if (!_attemptIps.ContainsKey(ip))
						_attemptIps.Add(ip, 1);
					else
						_attemptIps[ip] = _attemptIps[ip] + 1;
					// Log failure
					AuditMgr.AddAuditEntry(AuditType.Account, AuditSubtype.AccountFailedLogin, "", name);
					return null;
				}
				return account;
			}

			// create a new account
			account = new Account();
			account.Name = name;
			account.Password = hashedPassword;
			account.Realm = 0;
			account.CreationDate = DateTime.Now;
			account.LastLogin = DateTime.Now;
			account.Language = Properties.SERV_LANGUAGE;
			account.PrivLevel = 1;

			log.Info("New account created: " + name);

			GameServer.Database.AddObject(account);

			// Log account creation
			AuditMgr.AddAuditEntry(AuditType.Account, AuditSubtype.AccountCreate, "", name);
			return account;
		}
	}
}
