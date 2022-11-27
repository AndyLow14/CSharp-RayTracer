using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent an (infinite) plane in a scene.
    /// </summary>
    public class Sphere : SceneEntity
    {
        private Vector3 center;
        private double radius;
        private double t;
        private Material material;

        /// <summary>
        /// Construct a sphere given its center point and a radius.
        /// </summary>
        /// <param name="center">Center of the sphere</param>
        /// <param name="radius">Radius of the spher</param>
        /// <param name="material">Material assigned to the sphere</param>
        public Sphere(Vector3 center, double radius, Material material)
        {
            this.center = center;
            this.radius = radius;
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the sphere, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {

            //solutions for t if the ray intersects 
            double t0, t1;

            // geometric solution
            Vector3 L = center - ray.Origin;
            double tca = L.Dot(ray.Direction);
            if (tca < 0) return null;

            double d2 = L.Dot(L) - tca * tca;
            double radius2 = radius * radius;

            if (d2 > radius2) return null;

            double thc = Math.Sqrt(radius2 - d2);
            t0 = tca - thc;
            t1 = tca + thc;

            // analytic solution
            double a = ray.Direction.Dot(ray.Direction);
            double b = 2 * ray.Direction.Dot(L);

            double c = L.Dot(L) - radius2;
            if (!solveQuadratic(a, b, c, t0, t1)) return null;

            if (t0 > t1)
            {
                double tmp = t0;
                t0 = t1;
                t1 = tmp;
            }

            if (t0 < 0)
            {
                t0 = t1;  //if t0 is negative, let's use t1 instead 
                if (t0 < 0) return null;  //both t0 and t1 are negative 
            }

            t = t0;

            // compute the intersection point using equation 1
            Vector3 P = ray.Origin + t * ray.Direction;
            Vector3 Normal = (P - center).Normalized();

            return new RayHit(P, Normal, ray.Direction, material);  //this ray hits the sphere
        }

        public bool solveQuadratic(double a, double b, double c, double x0, double x1)
        {
            double discr = b * b - 4 * a * c;
            if (discr < 0) return false;
            else if (discr == 0) x0 = x1 = -0.5 * b / a;
            else
            {
                double q = (b > 0) ?
                    -0.5 * (b + Math.Sqrt(discr)) :
                    -0.5 * (b - Math.Sqrt(discr));
                x0 = q / a;
                x1 = c / q;
            }

            if (x0 > x1)
            {
                double tmp = x0;
                x0 = x1;
                x1 = tmp;
            }

            return true;
        }

        /// <summary>
        /// The material of the sphere.
        /// </summary>
        public Material Material { get { return this.material; } }

    }

}
