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
using DOL.Database;

namespace DOL.GS.Movement
{
	/// <summary>
	/// represents a point in a way path
	/// </summary>
	public class PathPoint
	{
		protected int m_maxspeed;
		protected PathPoint m_next = null;
		protected PathPoint m_prev = null;
		protected ePathType m_type;
		protected bool m_flag;
		protected int m_waitTime = 0;

		public Vector3 Position { get; set; }

		public PathPoint(PathPoint pp) : this(pp.Position, pp.MaxSpeed,pp.Type) {}

		public PathPoint(Vector3 p, int maxspeed, ePathType type) : this(p.X,  p.Y,  p.Z, maxspeed, type) {}

		public PathPoint(float x, float y, float z, int maxspeed, ePathType type)
		{
			Position = new Vector3(x, y, z);
			m_maxspeed = maxspeed;
			m_type = type;
			m_flag = false;
			m_waitTime = 0;
		}

		/// <summary>
		/// Speed allowed after that waypoint in forward direction
		/// </summary>
		public int MaxSpeed
		{
			get { return m_maxspeed; }
			set { m_maxspeed = value; }
		}

		/// <summary>
		/// next waypoint in path
		/// </summary>
		public PathPoint Next
		{
			get { return m_next; }
			set { m_next = value; }
		}

		/// <summary>
		/// previous waypoint in path
		/// </summary>
		public PathPoint Prev
		{
			get { return m_prev; }
			set { m_prev = value; }
		}

		/// <summary>
		/// flag toggle when go through pathpoint
		/// </summary>
		public bool FiredFlag
		{
			get { return m_flag; }
			set { m_flag = value; }
		}

		/// <summary>
		/// path type
		/// </summary>
		public ePathType Type
		{
			get { return m_type; }
			set { m_type = value; }
		}

		/// <summary>
		/// path type
		/// </summary>
		public int WaitTime
		{
			get { return m_waitTime; }
			set { m_waitTime = value; }
		}

		public PathPoint GetNearestNextPoint(Vector3 pos)
		{
			var nearest = this;
			var dist = Vector3.Distance(nearest.Position, pos);

			var pp = this;
			while (pp.Next != null)
			{
				pp = pp.Next;
				var d = Vector3.Distance(pp.Position, pos);
				if (d < dist)
				{
					nearest = pp;
					dist = d;
				}
			}

			return nearest;
		}
	}
}
