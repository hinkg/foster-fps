using System.Diagnostics;
using System.Numerics;
using Foster.Framework;
using Sledge.Formats;

namespace FosterTest;

public static class Collision
{
    public interface ISolid
    {
        AABB BoundingBox();
        bool IntersectAABB(AABB aabb);
        bool IntersectTriangle(Triangle triangle, out Vector3 penetrationNormal, out float penetrationDepth);
    }

    public struct Triangle
    {
        public Vector3 Point_0;
        public Vector3 Point_1;
        public Vector3 Point_2;

        public Triangle(Vector3 point_0, Vector3 point_1, Vector3 point_2)
        {
            Point_0 = point_0;
            Point_1 = point_1;
            Point_2 = point_2;
        }

        public readonly Vector3 this[int index] => index == 0 ? Point_0 : (index == 1 ? Point_1 : Point_2);

        public readonly Vector3 Edge_0 => Point_1 - Point_0;
        public readonly Vector3 Edge_1 => Point_2 - Point_1;
        public readonly Vector3 Edge_2 => Point_0 - Point_2;

        public readonly Vector3 EdgeByIndex(int index) => index == 0 ? Edge_0 : (index == 1 ? Edge_1 : Edge_2);

        public readonly Vector3 Normal => Vector3.Normalize(Vector3.Cross(Edge_1, Edge_2));

        public readonly AABB BoundingBox => AABB.FromBounds(
                    Vector3.Min(Vector3.Min(Point_0, Point_1), Point_2),
                    Vector3.Max(Vector3.Max(Point_0, Point_1), Point_2));
    }

    public struct AABB : ISolid
    {
        public Vector3 Position;
        public Vector3 Size;

        public AABB(Vector3 Position, Vector3 Size)
        {
            this.Position = Position;
            this.Size = Size;
        }

        public readonly Vector3 Max => Position + Size;
        public readonly Vector3 Min => Position + Size;

        public static AABB FromBounds(Vector3 min, Vector3 max) => new AABB(min, max - min);

        public readonly AABB BoundingBox() => this;

        public readonly bool IntersectAABB(AABB aabb) => IntersectAABBAABB(this, aabb);

        public readonly bool IntersectTriangle(Triangle triangle, out Vector3 penetrationNormal, out float penetrationDepth) => throw new NotImplementedException();
    }

    public static bool IntersectAABBAABB(AABB a, AABB b)
    {
        return a.Position.X < b.Position.X + b.Size.X &&
               a.Position.X + a.Size.X > b.Position.X &&
               a.Position.Y < b.Position.Y + b.Size.Y &&
               a.Position.Y + a.Size.Y > b.Position.Y &&
               a.Position.Z < b.Position.Z + b.Size.Z &&
               a.Position.Z + a.Size.Z > b.Position.Z;
    }

    public struct Sphere : ISolid
    {
        public Vector3 Origin;
        public float Radius;

        public Sphere(Vector3 inOrigin, float inRadius)
        {
            Origin = inOrigin;
            Radius = inRadius;
        }

        public readonly AABB BoundingBox() => AABB.FromBounds(Origin - Vector3.One * Radius, Origin + Vector3.One * Radius);

        public readonly bool IntersectAABB(AABB aabb) => throw new NotImplementedException();

        public readonly bool IntersectTriangle(Triangle tri, out Vector3 penetrationNormal, out float penetrationDepth) =>
            IntersectSphereTriangle(this, tri, out penetrationNormal, out penetrationDepth);

    }

    public static bool IntersectSphereTriangle(Sphere sphere, Triangle tri, out Vector3 penetrationNormal, out float penetrationDepth)
    {
        penetrationNormal = Vector3.Zero;
        penetrationDepth = 0.0f;

        float dist = Vector3.Dot(sphere.Origin - tri.Point_0, tri.Normal);

        // Pass through backface
        if (dist < 0.0)
            return false;

        if (Math.Abs(dist) > sphere.Radius)
            return false;

        var point0 = sphere.Origin - tri.Normal * dist;

        var c0 = Vector3.Cross(point0 - tri.Point_0, tri.Edge_0);
        var c1 = Vector3.Cross(point0 - tri.Point_1, tri.Edge_1);
        var c2 = Vector3.Cross(point0 - tri.Point_2, tri.Edge_2);

        bool inside = Vector3.Dot(c0, tri.Normal) <= 0 && Vector3.Dot(c1, tri.Normal) <= 0 && Vector3.Dot(c2, tri.Normal) <= 0;

        bool intersects = false;

        for (int i = 0; i < 3; i++)
        {
            var point = tri[i] + ClosestPointOnLineSegment(tri.EdgeByIndex(i), sphere.Origin - tri[i]);
            var vec = sphere.Origin - point;
            intersects |= vec.Length() < sphere.Radius;
        }

        if (inside || intersects)
        {
            Vector3 intersection = sphere.Origin - point0;

            if (!inside)
            {
                var bestDist = float.PositiveInfinity;

                for (int i = 0; i < 3; i++)
                {
                    var newVec = sphere.Origin - (tri[i] + ClosestPointOnLineSegment(tri.EdgeByIndex(i), sphere.Origin - tri[i]));
                    var newDist = newVec.Length();

                    if (newDist < bestDist)
                    {
                        bestDist = newDist;
                        intersection = newVec;
                    }
                }
            }

            penetrationNormal = intersection.Normalise();
            penetrationDepth = sphere.Radius - intersection.Length();

            return true;
        }

        return false;
    }

    public struct Capsule : ISolid
    {
        public Vector3 Base;
        public float Radius;
        public float Height;

        public Capsule(Vector3 inBase, float inRadius, float inHeight)
        {
            Base = inBase;
            Radius = MathF.Min(inRadius, inHeight / 2.0f);
            Height = inHeight;
        }

        public readonly Vector3 Top => Base + Normal * Height;
        public readonly Vector3 Normal => Vector3.UnitZ;

        public readonly Sphere BaseSphere => new(Base + Normal * Radius, Radius);
        public readonly Sphere TopSphere => new(Top - Normal * Radius, Radius);

        public readonly AABB BoundingBox() => AABB.FromBounds(Base - Vector3.One * Radius, Top + Vector3.One * Radius);

        public readonly bool IntersectAABB(AABB aabb) => throw new NotImplementedException();

        public readonly bool IntersectTriangle(Triangle tri, out Vector3 penetrationNormal, out float penetrationDepth) =>
            IntersectCapsuleTriangle(this, tri, out penetrationNormal, out penetrationDepth);
    }

    public static bool IntersectCapsuleTriangle(Capsule capsule, Triangle tri, out Vector3 penetrationNormal, out float penetrationDepth)
    {
        bool parallel = Vector3.Dot(capsule.Normal, tri.Normal) < 0.001;

        Vector3 referencePoint = Vector3.Zero;

        if (parallel)
        {
            referencePoint = tri.Point_0;
        }
        else
        {
            var t = Vector3.Dot(tri.Normal, (tri.Point_0 - capsule.Base) / Math.Abs(Vector3.Dot(tri.Normal, capsule.Normal)));
            var linePlaneIntersection = capsule.Base + capsule.Normal * t;

            var c0 = Vector3.Cross(linePlaneIntersection - tri.Point_0, tri.Edge_0);
            var c1 = Vector3.Cross(linePlaneIntersection - tri.Point_1, tri.Edge_1);
            var c2 = Vector3.Cross(linePlaneIntersection - tri.Point_2, tri.Edge_2);

            bool inside = Vector3.Dot(c0, tri.Normal) <= 0 && Vector3.Dot(c1, tri.Normal) <= 0 && Vector3.Dot(c2, tri.Normal) <= 0;

            if (inside)
            {
                referencePoint = linePlaneIntersection;
            }
            else
            {
                var bestDist = float.PositiveInfinity;

                for (int i = 0; i < 3; i++)
                {
                    var point = ClosestPointOnLineSegment(tri.EdgeByIndex(i), linePlaneIntersection - tri[i]);
                    if (point.Length() < bestDist)
                    {
                        bestDist = point.Length();
                        referencePoint = tri[i] + point;
                    }
                }
            }
        }

        // The center of the best sphere candidate:
        Vector3 center = ClosestPointOnLineSegment(capsule.BaseSphere.Origin, capsule.TopSphere.Origin, referencePoint);

        return IntersectSphereTriangle(new Sphere(center, capsule.Radius), tri, out penetrationNormal, out penetrationDepth);
    }

    public static Vector3 ClosestPointOnLineSegment(Vector3 A, Vector3 B, Vector3 Point)
    {
        var AB = B - A;
        float t = Vector3.Dot(Point - A, AB) / Vector3.Dot(AB, AB);
        return A + Math.Clamp(t, 0.0f, 1.0f) * AB;
    }

    public static Vector3 ClosestPointOnLineSegment(Vector3 AB, Vector3 Point)
    {
        float t = Vector3.Dot(Point, AB) / Vector3.Dot(AB, AB);
        return Math.Clamp(t, 0.0f, 1.0f) * AB;
    }
}
