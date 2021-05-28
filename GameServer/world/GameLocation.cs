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
using System.Numerics;

namespace DOL.GS
{
	/// <summary>
	/// 
	/// </summary>
	public class GameLocation : IGameLocation
	{
		protected ushort m_regionId;
		protected ushort m_heading;
		protected String m_name;
		
		public GameLocation(string name, GameObject obj) : this(name, obj.CurrentRegionID, obj.Position, obj.Heading)
        {
        }
		public GameLocation(string name, ushort regionId, float x, float y, float z, ushort heading)
			: this(name, regionId, new Vector3(x, y, z), heading)
        {
        }
		public GameLocation(String name, ushort regionId, ushort zoneId, float x, float y, float z, ushort heading)
			: this(name, regionId, ConvertLocalXToGlobalX(x, zoneId), ConvertLocalYToGlobalY(y, zoneId), z, heading)
		{
		}

		public GameLocation(String name, ushort regionId, float x, float y, float z) : this(name, regionId, x, y, z, 0)
		{
		}

		public GameLocation(String name, ushort regionId, Vector3 position, ushort heading)
		{
			m_regionId = regionId;
			m_name = name;
			m_heading = heading;
			Position = position;
		}

		public Vector3 Position { get; set; }

		/// <summary>
		/// heading of this point
		/// </summary>
		public ushort Heading
		{
			get { return m_heading; }
			set { m_heading = value; }
		}

		/// <summary>
		/// RegionID of this point
		/// </summary>
		public ushort RegionID
		{
			get { return m_regionId; }
			set { m_regionId = value; }
		}

		/// <summary>
		/// Name of this point
		/// </summary>
		public String Name
		{
			get { return m_name; }
			set { m_name = value; }
		}
	
		/// <summary>
		/// calculates distance between 2 points
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		public float GetDistance( IGameLocation location )
		{
			if (this.RegionID == location.RegionID)
			{
				return Vector3.Distance(Position, location.Position);
			}
			else
			{
				return -1;
			}
		}

		public static float ConvertLocalXToGlobalX(float localX, ushort zoneId)
		{
			Zone z = WorldMgr.GetZone(zoneId);
			return z.XOffset + localX;
		}

		public static float ConvertLocalYToGlobalY(float localY, ushort zoneId)
		{
			Zone z = WorldMgr.GetZone(zoneId);
			return z.YOffset + localY;
		}

		public static float ConvertGlobalXToLocalX(float globalX, ushort zoneId)
		{
			Zone z = WorldMgr.GetZone(zoneId);
			return globalX - z.XOffset;
		}

		public static float ConvertGlobalYToLocalY(float globalY, ushort zoneId)
		{
			Zone z = WorldMgr.GetZone(zoneId);
			return globalY - z.YOffset;
		}

        [Obsolete( "Use instance method GetDistance( IGameLocation location )" )]
        public static float GetDistance(ushort r1, float x1, float y1, float z1, ushort r2, float x2, float y2, float z2 )
        {
            GameLocation loc1 = new GameLocation( "loc1", r1, x1, y1, z1 );
            GameLocation loc2 = new GameLocation( "loc2", r2, x2, y2, z2 );

            return loc1.GetDistance( loc2 );
        }
	}
}
