using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent an (infinite) plane in a scene.
    /// </summary>
    public class Plane : SceneEntity
    {
        private Vector3 center;
        private Vector3 normal;
        private Material material;
        private double t;

        /// <summary>
        /// Construct an infinite plane object.
        /// </summary>
        /// <param name="center">Position of the center of the plane</param>
        /// <param name="normal">Direction that the plane faces</param>
        /// <param name="material">Material assigned to the plane</param>
        public Plane(Vector3 center, Vector3 normal, Material material)
        {
            this.center = center;
            this.normal = normal;
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the plane, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            // assuming vectors are all normalized
            double denom = normal.Dot(ray.Direction);
            if (denom < 1e-6)
            {
                Vector3 p0l0 = center - ray.Origin;
                t = p0l0.Dot(normal) / denom;
                // compute the intersection point using equation 1
                Vector3 P = ray.Origin + t * ray.Direction;
                //Vector3 Normal = (center - P).Normalized();

                return new RayHit(P, normal, ray.Direction, material);
            }

            return null;
        }

        /// <summary>
        /// The material of the plane.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
