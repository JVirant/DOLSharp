namespace System.Numerics
{
	public static class Vector3Exts
	{
		public static bool IsInRange(this Vector3 source, Vector3 target, float range)
		{
			range = range * range;
			return Vector3.DistanceSquared(source, target) <= range;
		}

		// Dre: I don't understand what this function should do...
		public static Vector3 To2D(this Vector3 v)
		{
			return new Vector3(v.X, v.Y, 0);
		}
	}
}
