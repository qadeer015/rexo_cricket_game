using System;

namespace CricketBlitz.Sim
{
    /// <summary>
    /// Small float vector so the match rules can run without the Unity engine.
    /// </summary>
    public struct Vec3
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float Magnitude
        {
            get { return (float)Math.Sqrt(X * X + Y * Y + Z * Z); }
        }

        public float MagnitudeXZ
        {
            get { return (float)Math.Sqrt(X * X + Z * Z); }
        }

        public static Vec3 operator +(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vec3 operator -(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vec3 operator *(Vec3 a, float s)
        {
            return new Vec3(a.X * s, a.Y * s, a.Z * s);
        }

        public static Vec3 operator *(float s, Vec3 a)
        {
            return a * s;
        }

        public static float Distance(Vec3 a, Vec3 b)
        {
            return (a - b).Magnitude;
        }

        public static float DistanceXZ(Vec3 a, Vec3 b)
        {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public static Vec3 NormalizeXZ(Vec3 v)
        {
            float m = v.MagnitudeXZ;
            if (m < 0.0001f)
            {
                return new Vec3(0f, 0f, 0f);
            }

            return new Vec3(v.X / m, 0f, v.Z / m);
        }
    }
}
