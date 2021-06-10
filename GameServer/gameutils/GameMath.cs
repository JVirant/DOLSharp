using System;
using System.Numerics;
using DOL.GS;

namespace DOL
{
	public static class GameMath
	{
		public static Vector2 ToVector2(this Vector3 v) => new Vector2(v.X, v.Y);

		/// <summary>
		/// The factor to convert a heading value to radians
		/// </summary>
		/// <remarks>
		/// Heading to degrees = heading * (360 / 4096)
		/// Degrees to radians = degrees * (PI / 180)
		/// </remarks>
		public const float HEADING_TO_RADIAN = (360.0f / 4096.0f) * ((float)Math.PI / 180.0f);

		/// <summary>
		/// The factor to convert radians to a heading value
		/// </summary>
		/// <remarks>
		/// Radians to degrees = radian * (180 / PI)
		/// Degrees to heading = degrees * (4096 / 360)
		/// </remarks>
		public const float RADIAN_TO_HEADING = (180.0f / (float)Math.PI) * (4096.0f / 360.0f);

		// Coordinate calculation functions in DOL are standard trigonometric functions, but
		// with some adjustments to account for the different coordinate system that DOL uses
		// compared to the standard Cartesian coordinates used in trigonometry.
		//
		// Cartesian grid:
		//        90
		//         |
		// 180 --------- 0
		//         |
		//        270
		//        
		// DOL Heading grid:
		//       2048
		//         |
		// 1024 ------- 3072
		//         |
		//         0
		// 
		// The Cartesian grid is 0 at the right side of the X-axis and increases counter-clockwise.
		// The DOL Heading grid is 0 at the bottom of the Y-axis and increases clockwise.
		// General trigonometry and the System.Math library use the Cartesian grid.

		public static ushort GetHeading(Vector2 origin, Vector3 target)
			=> GetHeading(origin, target.ToVector2());
		public static ushort GetHeading(Vector3 origin, Vector3 target)
			=> GetHeading(origin.ToVector2(), target.ToVector2());
		public static ushort GetHeading(Vector3 origin, Vector2 target)
			=> GetHeading(origin.ToVector2(), target);
		/// <summary>
		/// Get the heading to a point
		/// </summary>
		/// <param name="origin">Source point</param>
		/// <param name="target">Target point</param>
		/// <returns>Heading to target point</returns>
		public static ushort GetHeading(Vector2 origin, Vector2 target)
		{
			float dx = target.X - origin.X;
			float dy = target.Y - origin.Y;

			float heading = (float)Math.Atan2(-dx, dy) * RADIAN_TO_HEADING;

			if (heading < 0)
				heading += 4096;

			return (ushort) heading;
		}

		public static float GetAngle(GameObject origin, GameObject target)
			=> GetAngle(origin.Position.ToVector2(), origin.Heading, target.Position.ToVector2());
		public static float GetAngle(Vector2 origin, ushort originHeading, Vector2 target)
		{
			float headingDifference = (GetHeading(origin, target) & 0xFFF) - (originHeading & 0xFFF);

			if (headingDifference < 0)
				headingDifference += 4096.0f;

			return (headingDifference * 360.0f / 4096.0f);
		}

		public static Vector2 GetPointFromHeading(GameObject origin, float distance)
			=> GetPointFromHeading(origin.Position.ToVector2(), origin.Heading, distance);
		public static Vector2 GetPointFromHeading(Vector3 origin, ushort heading, float distance)
			=> GetPointFromHeading(origin.ToVector2(), heading, distance);
		public static Vector2 GetPointFromHeading(Vector2 origin, ushort heading, float distance)
		{
			float angle = heading * HEADING_TO_RADIAN;
			float targetX = origin.X - ((float)Math.Sin(angle) * distance);
			float targetY = origin.Y + ((float)Math.Cos(angle) * distance);

			var point = new Vector2();
			if (targetX > 0)
				point.X = targetX;
			if (targetY > 0)
				point.Y = targetY;
			return point;
		}

		/// <summary>
		/// Get the distance without Z between two points
		/// </summary>
		/// <remarks>
		/// If you don't actually need the distance value, it is faster
		/// to use IsWithinRadius (since it avoids the square root calculation)
		/// </remarks>
		/// <param name="b">Source point</param>
		/// <param name="a">Target point</param>
		/// <returns>Distance to point</returns>
		public static float GetDistance2D(Vector3 a, Vector3 b)
		{
			var vec = (b - a);
			vec.Z = 0; // 2D
			return vec.Length();
		}

		public static bool IsWithinRadius(GameObject source, GameObject target, float distance)
		{
			if (source.CurrentRegion != target.CurrentRegion)
				return false;
			return Vector3.DistanceSquared(source.Position, target.Position) <= distance * distance;
		}
		public static bool IsWithinRadius(GameObject source, Vector3 target, float distance)
			=> Vector3.DistanceSquared(source.Position, target) <= distance * distance;
		public static bool IsWithinRadius(Vector3 source, Vector3 target, float distance)
			=> Vector3.DistanceSquared(source, target) <= distance * distance;

		public static bool IsWithinRadius2D(GameObject source, GameObject target, float distance)
		{
			if (source.CurrentRegion != target.CurrentRegion)
				return false;
			return Vector2.DistanceSquared(source.Position.ToVector2(), target.Position.ToVector2()) <= distance * distance;
		}
		public static bool IsWithinRadius2D(GameObject source, Vector3 target, float distance)
			=> Vector2.DistanceSquared(source.Position.ToVector2(), target.ToVector2()) <= distance * distance;
		public static bool IsWithinRadius2D(Vector3 source, Vector3 target, float distance)
			=> Vector2.DistanceSquared(source.ToVector2(), target.ToVector2()) <= distance * distance;
	}
}