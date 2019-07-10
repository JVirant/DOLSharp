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
using System.IO;
using System.Reflection;

using DOL.GS;
using DOL.Database.Connection;

using NUnit.Framework;
using DOL.GS.ServerProperties;
using DOL.Database;

namespace DOL.Server.Tests
{
	/// <summary>
	/// SetUpTests Start The Needed Environnement for Unit Tests
	/// </summary>
	[SetUpFixture]
	public class SetUpTests
	{
		public SetUpTests()
		{
		}
		
		/// <summary>
		/// Create Game Server Instance for Tests
		/// </summary>
		public static void CreateGameServerInstance()
		{
			Console.WriteLine("Create Game Server Instance");
			DirectoryInfo FakeRoot = new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory;
			Console.WriteLine("Fake Root: " + FakeRoot.FullName);
			
			if(GameServer.Instance == null)
			{
				try
				{
					File.Delete(Path.Combine(FakeRoot.FullName, "dol-tests-only.sqlite3.db"));
				}
				catch { }
				GameServerConfiguration config = new GameServerConfiguration();
				config.RootDirectory = FakeRoot.FullName;
				config.DBType = ConnectionType.DATABASE_SQLITE;
				config.DBConnectionString = string.Format("Data Source={0};Version=3;Pooling=False;Cache Size=1073741824;Journal Mode=Off;Synchronous=Off;Foreign Keys=True;Default Timeout=60",
			                                     Path.Combine(config.RootDirectory, "dol-tests-only.sqlite3.db"));
				config.Port = 0; // Auto Choosing Listen Port
				config.UDPPort = 0; // Auto Choosing Listen Port
				config.IP = System.Net.IPAddress.Parse("127.0.0.1");
				config.UDPIP = System.Net.IPAddress.Parse("127.0.0.1");
				config.RegionIP = System.Net.IPAddress.Parse("127.0.0.1");
				config.EnableCompilation = false;
				GameServer.CreateInstance(config);
				CreateTestDatabaseObjects();
				Console.WriteLine("Game Server Instance Created !");
			}
		}
		
		[OneTimeSetUp]
		public virtual void Init()
		{
			CreateGameServerInstance();
			
			if (!GameServer.Instance.IsRunning)
			{
				Console.WriteLine("Starting GameServer");
				if (!GameServer.Instance.Start())
				{
					Console.WriteLine("Error init GameServer");
					throw new Exception("Error init GameServer");
				}
			}
			else
			{
				Console.WriteLine("GameServer already running, skip init of Gameserver...");
			}
		}

		[OneTimeTearDown]
		public void Dispose()
		{
			GameServer.Instance.Stop();
		}

		private static void CreateTestDatabaseObjects()
		{
			// Server property to stop loading quests
			GameServer.Database.AddObject(new ServerProperty { Category = "system", Key = "load_quests", Value = "false", DefaultValue = "true", Description = "load_quests" });
			GameServer.Database.AddObject(new DBRegions
			{
				RegionID = 1,
				Name = "Region001",
				Description = "Albion",
				IP = "127.0.0.1",
				Port = 10400,
				Expansion = 0,
				HousingEnabled = false,
				DivingEnabled = true,
				WaterLevel = 0,
				IsFrontier = false,
			});
			GameServer.Database.AddObject(new Zones
			{
				ZoneID = 0,
				RegionID = 1,
				Name = "Camelot Hills",
				IsLava = false,
				DivingFlag = 0,
				WaterLevel = 0,
				OffsetY = 0,
				OffsetX = 0,
				Width = 8,
				Height = 8,
				Experience = 0,
				Realmpoints = 0,
				Bountypoints = 0,
				Coin = 0,
				Realm = 0,
			});
			GameServer.Database.AddObject(new Account
			{
				Name = "test",
				Language = "EN",
				Password = "test",
				Realm = 1,
				Status = 0,
			});
			GameServer.Database.AddObject(new DOLCharacters
			{
				AccountName = "test",
				AccountSlot = 1,
				Name = "Test",
				Xpos = 4000,
				Ypos = 4000,
				Zpos = 4000,
				Region = 1,
			});
		}
	}
}
