using System.Numerics;

namespace DOL.GS.Geometry
{
	public class OrientedBoundingBox : ICollider
	{
		public readonly Vector3 HalfExtents;
		public readonly Matrix4x4 Transformation;

		public Vector3 Center => Transformation.Translation;
		public AABoundingBox Box => new AABoundingBox(Center + -HalfExtents * 1.414213f, Center + HalfExtents * 1.414213f);

		public OrientedBoundingBox(Vector3 center, Vector3 halfExtents, Quaternion orientation)
		{
			HalfExtents = halfExtents;
			Transformation = Matrix4x4.CreateFromQuaternion(orientation) * Matrix4x4.CreateTranslation(center);
		}

		public bool ContainsPoint(Vector3 point)
		{
			Matrix4x4.Invert(Transformation, out var invMatrix);
			var local = Vector3.Abs(Vector3.Transform(point, invMatrix));
			return local.X <= HalfExtents.X && local.Y <= HalfExtents.Y && local.Z <= HalfExtents.Z;
		}

		public float CollideWithRay(Vector3 origin, Vector3 direction, float maxDistance)
		{
			RaycastStats stats = new RaycastStats();
			return CollideWithRay(origin, direction, maxDistance, ref stats);
		}
		public float CollideWithRay(Vector3 origin, Vector3 direction, float maxDistance, ref RaycastStats stats)
		{
			Matrix4x4.Invert(Transformation, out var invMatrix);
			origin = Vector3.Transform(origin, invMatrix);
			direction = Vector3.TransformNormal(direction, invMatrix);
			return new AABoundingBox(-HalfExtents, HalfExtents).CollideWithRay(origin, direction, maxDistance, ref stats);
		}

		public bool CollideWithAABB(AABoundingBox box)
		{
			return false;
		}
	}
}
