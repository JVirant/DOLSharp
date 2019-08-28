using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Scripts;

namespace DOL.GS.Scripts
{
	public class FollowingMob : AmteMob
	{
		private DBBrainsParam m_param;
		public string FollowMobID = "";

		private double m_Angle;
		private int m_DistMob;

		private GameNPC m_mobFollow;
		public GameNPC MobFollow
		{
			get { return m_mobFollow; }
			set
			{
				m_mobFollow = value;
				if (value == null)
					return;

				double DX = SpawnPoint.X - value.SpawnPoint.X;
				double DY = SpawnPoint.Y - value.SpawnPoint.Y;
				m_DistMob = (int)Math.Sqrt(DX * DX + DY * DY);

				if (m_DistMob > 0)
				{
					m_Angle = Math.Asin(DX / m_DistMob);
					if (DY > 0) m_Angle += (Math.PI / 2 - m_Angle) * 2;
					m_Angle -= Math.PI / 2;

					m_Angle = (m_Angle - value.SpawnHeading / RADIAN_TO_HEADING) % (Math.PI * 2);
				}
				else m_Angle = 0;
			}
		}

		public override bool IsVisibleToPlayers
		{
			get { return true; }
		}

		public override bool AddToWorld()
		{
			if (!base.AddToWorld())
				return false;
			FollowingBrain brain = new FollowingBrain();
			if (Brain is IOldAggressiveBrain)
			{
				brain.AggroLevel = ((IOldAggressiveBrain)Brain).AggroLevel;
				brain.AggroRange = ((IOldAggressiveBrain)Brain).AggroRange;
				//brain.AggroLink = ((IAggressiveBrain)Brain).AggroLink;
			}
			SetOwnBrain(brain);
			return true;
		}

		#region DB
		public override void LoadFromDatabase(DataObject obj)
		{
			base.LoadFromDatabase(obj);
			m_param = GameServer.Database.SelectObject<DBBrainsParam>("`MobID` = '" + obj.ObjectId + "'");
			if (m_param != null && m_param.Param == "MobIDToFollow")
				FollowMobID = m_param.Value;
		}

		public override void SaveIntoDatabase()
		{
			base.SaveIntoDatabase();
			if (FollowMobID != "" && m_param == null)
			{
				m_param = new DBBrainsParam { MobID = InternalID, Param = "MobIDToFollow", Value = FollowMobID };
				GameServer.Database.AddObject(m_param);
			}
			else if (FollowMobID != "" && m_param.Value != FollowMobID)
			{
				m_param.Value = FollowMobID;
				GameServer.Database.SaveObject(m_param);
			}
		}

		public override void DeleteFromDatabase()
		{
			base.DeleteFromDatabase();
			if (m_param != null)
				GameServer.Database.DeleteObject(m_param);
		}
		#endregion

		public override void StartAttack(GameObject attackTarget)
		{
			if (MobFollow != null)
				StopFollowing();
			base.StartAttack(attackTarget);
		}

		protected override int FollowTimerCallback(RegionTimer callingTimer)
		{
			if (IsCasting)
				return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
			bool wasInRange = m_followTimer.Properties.getProperty(FOLLOW_TARGET_IN_RANGE, false);
			m_followTimer.Properties.removeProperty(FOLLOW_TARGET_IN_RANGE);

			GameObject followTarget = (GameObject)m_followTarget.Target;
			GameLiving followLiving = followTarget as GameLiving;
			if (followLiving != null && !followLiving.IsAlive)
			{
				StopFollowing();
				Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(followTarget));
				return 0;
			}

			//Stop following if we have no target
			if (followTarget == null || followTarget.ObjectState != eObjectState.Active || CurrentRegionID != followTarget.CurrentRegionID)
			{
				StopFollowing();
				Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(followTarget));
				return 0;
			}

			//Calculate the difference between our position and the players position
			var diffx = (double)followTarget.X - X;
			var diffy = (double)followTarget.Y - Y;
			var diffz = (double)followTarget.Z - Z;

			//SH: Removed Z checks when one of the two Z values is zero(on ground)
			double distance;
			if (followTarget.Z == 0 || Z == 0)
				distance = Math.Sqrt(diffx * diffx + diffy * diffy);
			else
				distance = Math.Sqrt(diffx * diffx + diffy * diffy + diffz * diffz);

			//if distance is greater then the max follow distance, stop following and return home
			if (distance > m_followMaxDist)
			{
				StopFollowing();
				Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(followTarget));
				WalkToSpawn();
				return 0;
			}

			//if the npc hasn't hit or been hit in a while, stop following and return home
			if (Brain is StandardMobBrain && Brain is IControlledBrain == false)
			{
				StandardMobBrain brain = Brain as StandardMobBrain;
				if (AttackState && brain != null && followLiving != null)
				{
					// Amtenael MODIF : Aggro entre 30 et 45 secondes
					//long seconds = Util.Random(30, 45);
					long seconds = 20 + ((brain.GetAggroAmountForLiving(followLiving) / (MaxHealth + 1)) * 100);
					long lastattacked = LastAttackTick;
					long lasthit = LastAttackedByEnemyTick;
					if (CurrentRegion.Time - lastattacked > seconds * 1000 && CurrentRegion.Time - lasthit > seconds * 1000)
					{
						StopFollowing();
						Notify(GameNPCEvent.FollowLostTarget, this, new FollowLostTargetEventArgs(followTarget));
						brain.ClearAggroList();
						WalkToSpawn();
						return 0;
					}
				}
			}

			//Are we in range yet?
			if ((followTarget == MobFollow && distance - 5 <= m_DistMob && m_DistMob <= distance + 5)
				|| (followTarget != MobFollow && distance <= m_followMinDist))
			{
				//StopMoving();
				if (followTarget != MobFollow) TurnTo(followTarget);
				else TurnTo(followTarget.Heading);

				if (!wasInRange)
				{
					m_followTimer.Properties.setProperty(FOLLOW_TARGET_IN_RANGE, true);
					FollowTargetInRange();
				}
				return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
			}

			// follow on distance
			diffx = (diffx / distance) * m_followMinDist;
			diffy = (diffy / distance) * m_followMinDist;
			diffz = (diffz / distance) * m_followMinDist;

			int newX = (int)(followTarget.X - diffx);
			int newY = (int)(followTarget.Y - diffy);
			int newZ = (int)(followTarget.Z - diffz);

			if (followTarget == MobFollow)
			{
				double angle = (m_Angle + followTarget.Heading / RADIAN_TO_HEADING) % (Math.PI * 2);

				newX = (int)(followTarget.X + Math.Cos(angle) * m_DistMob);
				newY = (int)(followTarget.Y + Math.Sin(angle) * m_DistMob);

				var speed = MaxSpeed;
				if (GetDistance(new Point2D(newX, newY)) < 500)
					speed = (short)Math.Max(MaxSpeed, MobFollow.CurrentSpeed + 6);
				WalkTo(newX, newY, newZ, speed);
			}
			else
				WalkTo(newX, newY, (ushort)newZ, MaxSpeed);
			return ServerProperties.Properties.GAMENPC_FOLLOWCHECK_TIME;
		}

		public override IList<string> DelveInfo()
		{
			var info = base.DelveInfo();
			info.Add("");
			info.Add("-- FollowingMob --");
			info.Add(" + Mob à suivre: " + MobFollow.Name + " (Realm: " + MobFollow.Realm + ", Guilde: " + MobFollow.GuildName + ")");
			return info;
		}
	}
}

namespace DOL.AI.Brain
{
	public class FollowingBrain : AmteMobBrain
	{
		private string FollowMobID
		{
			get { return ((FollowingMob)Body).FollowMobID; }
		}

		public override bool Start()
		{
			if (!base.Start()) return false;
			if (FollowMobID != "")
				SetMobByMobID();
			if (((FollowingMob)Body).MobFollow != null)
				Body.Follow(((FollowingMob)Body).MobFollow, 10, 3000);
			return true;
		}

		public override void Think()
		{
			if (!Body.IsCasting && Body.Spells != null && Body.Spells.Count > 0)
				CheckSpells(eCheckSpellType.Defensive);

			if (AggroLevel > 0)
			{
				CheckPlayerAggro();
				CheckNPCAggro();
			}

			if (!Body.AttackState && !Body.IsCasting && !Body.IsMoving
				&& Body.Heading != Body.SpawnHeading && Body.X == Body.SpawnPoint.X
				&& Body.Y == Body.SpawnPoint.Y && Body.Z == Body.SpawnPoint.Z)
				Body.TurnTo(Body.SpawnHeading);

			if (!Body.InCombat)
				Body.TempProperties.removeProperty(GameLiving.LAST_ATTACK_DATA);

			if (Body.CurrentSpellHandler != null || Body.IsMoving || Body.AttackState ||
				Body.InCombat || Body.IsMovingOnPath || Body.CurrentFollowTarget != null)
				return;
			if (((FollowingMob)Body).MobFollow == null && FollowMobID != "")
				SetMobByMobID();

			if (((FollowingMob)Body).MobFollow != null)
				Body.Follow(((FollowingMob)Body).MobFollow, 10, 3000);
			else
				Body.WalkToSpawn();
		}

		private void SetMobByMobID()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(3000))
				if (npc.InternalID == FollowMobID)
				{
					((FollowingMob)Body).MobFollow = npc;
					break;
				}
		}
	}
}
