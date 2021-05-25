using System.Numerics;

namespace DOL.GS
{
	static class Extensions
	{
		public static float[] ToRecastFloats(this Vector3 value)
		{
			return new[] { value.X * LocalPathingMgr.CONVERSION_FACTOR, value.Z * LocalPathingMgr.CONVERSION_FACTOR, value.Y * LocalPathingMgr.CONVERSION_FACTOR };
		}
	}
}
