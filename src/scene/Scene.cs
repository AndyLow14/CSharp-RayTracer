using System;
using System.Diagnostics;
using System.Collections.Generic;

// Code referenced from Scratchapixel, https://www.scratchapixel.com/lessons/3d-basic-rendering/introduction-to-ray-tracing

namespace RayTracer
{
    /// <summary>
    /// Class to represent a ray traced scene, including the objects,
    /// light sources, and associated rendering logic.
    /// </summary>
    public class Scene
    {
        private SceneOptions options;
        private ISet<SceneEntity> entities;
        private ISet<PointLight> lights;
        private double fov = 60f;
        Random rand = new Random();
        Stopwatch stopWatch = new Stopwatch();

        /// <summary>
        /// Construct a new scene with provided options.
        /// </summary>
        /// <param name="options">Options data</param>
        public Scene(SceneOptions options = new SceneOptions())
        {
            this.options = options;
            this.entities = new HashSet<SceneEntity>();
            this.lights = new HashSet<PointLight>();
        }

        /// <summary>
        /// Add an entity to the scene that should be rendered.
        /// </summary>
        /// <param name="entity">Entity object</param>
        public void AddEntity(SceneEntity entity)
        {
            this.entities.Add(entity);
        }

        /// <summary>
        /// Add a point light to the scene that should be computed.
        /// </summary>
        /// <param name="light">Light structure</param>
        public void AddPointLight(PointLight light)
        {
            this.lights.Add(light);
        }

        /// <summary>
        /// Render the scene to an output image. This is where the bulk
        /// of your ray tracing logic should go... though you may wish to
        /// break it down into multiple functions as it gets more complex!
        /// </summary>
        /// <param name="outputImage">Image to store render output</param>
        public void Render(Image outputImage)
        {
            stopWatch.Start();

            int aliasMultiplier = options.AAMultiplier;
            int depth = 0;

            for (int y = 0; y < outputImage.Height; y++)
            {
                float progress = ((float)y / outputImage.Height) * 100 + 1;
                if (progress == Math.Round(progress, 0))
                {
                    Console.WriteLine("Rendering - " + progress + "%");
                }

                for (int x = 0; x < outputImage.Width; x++)
                {
                    double imageAspectRatio = outputImage.Width / outputImage.Height;  // Assuming width > height 
                    double Px = (2 * ((x + 0.5) / outputImage.Width) - 1) * Math.Tan(fov / 2 * Math.PI / 180) * imageAspectRatio;
                    double Py = (1 - 2 * ((y + 0.5) / outputImage.Height)) * Math.Tan(fov / 2 * Math.PI / 180);
                    Vector3 rayOrigin = new Vector3(0, 0, 1e-4);
                    Vector3 rayDirection = new Vector3(Px, Py, 1);
                    Color finalColor = new Color(0, 0, 0);

                    int effects = 0;

                    if (options.ApertureRadius > 0)
                    {
                        Color colorDOF = depthOfField(rayOrigin, rayDirection, depth);
                        finalColor += colorDOF;
                        effects++;
                    }

                    if (aliasMultiplier > 1)
                    {
                        Color colorAA = antiAlias(aliasMultiplier, outputImage, x, y, rayOrigin);
                        finalColor += colorAA;
                        effects++;
                    }

                    if (effects == 0)
                    {
                        // No anti-aliasing
                        Color colorNoAA = castRay(rayOrigin, new Vector3(Px, Py, 1));
                        finalColor += colorNoAA;
                        effects++;
                    }

                    // Output colour for sum of all lights after anti-aliasing and DOF calculations
                    outputImage.SetPixel(x, y, finalColor / effects);
                }

            }

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            Console.WriteLine("Execution time = " + ts.ToString("mm\\:ss\\.ff"));

        }

        public Color depthOfField(Vector3 rayOrigin, Vector3 rayDirection, int depth)
        {
            // Depth of field
            int sampleValue = 160;
            double apertureSize = options.ApertureRadius;
            double focusDistance = options.FocalLength;
            Color sumColorDOF = new Color(0, 0, 0);
            Vector3 focalPoint = rayOrigin + focusDistance * rayDirection;

            for (int i = 0; i < sampleValue; i++)
            {
                // Select random x and y values in a circle with centre at origin of radius == ApertureRadius
                double r = apertureSize * Math.Sqrt(rand.NextDouble());
                double theta = rand.NextDouble() * 2 * Math.PI;

                double rw = r * Math.Cos(theta);
                double rh = r * Math.Sin(theta);

                // Shoot multiple rays from random locations through the same focal point
                Vector3 dir = focalPoint - new Vector3(rw, rh, 1e-4);

                Color blend = castRay(new Vector3(rw, rh, 1e-4), dir, depth + 1);

                sumColorDOF += blend;
            }

            // Average all the rays shot from the aperture through the focal point
            sumColorDOF /= sampleValue;

            return sumColorDOF;
        }

        public Color antiAlias(int aliasMultiplier, Image outputImage, int x, int y, Vector3 rayOrigin)
        {
            Color sumColorAA = new Color(0, 0, 0);

            // Anti-alias by shooting multiple rays per pixel and obtaining an average colour
            for (int aliasY = 0; aliasY < aliasMultiplier; aliasY++)
            {
                for (int aliasX = 0; aliasX < aliasMultiplier; aliasX++)
                {
                    double imageAspectRatio = outputImage.Width / outputImage.Height;  // Assuming width > height 
                    double aliasPx = (2 * ((x + (double)aliasX / (aliasMultiplier)) / outputImage.Width) - 1) * Math.Tan(fov / 2 * Math.PI / 180) * imageAspectRatio;
                    double aliasPy = (1 - 2 * ((y + (double)aliasY / (aliasMultiplier)) / outputImage.Height)) * Math.Tan(fov / 2 * Math.PI / 180);
                    Color outputColor = castRay(rayOrigin, new Vector3(aliasPx, aliasPy, 1));
                    sumColorAA += outputColor;
                }
            }

            sumColorAA /= (aliasMultiplier * aliasMultiplier);

            return sumColorAA;
        }

        public Color castRay(Vector3 rayOrigin, Vector3 rayDirection, int depth = 0)
        {

            if (depth > 10) return new Color(0, 0, 0);

            Ray ray = new Ray(rayOrigin, rayDirection.Normalized());

            // Find the closest entity to the camera
            double tNear = Double.PositiveInfinity;
            SceneEntity closestEntity = null;
            Vector3 hitPosition = new Vector3(0, 0, 0);
            RayHit closestHitRay = null;

            foreach (SceneEntity entity in this.entities)
            {
                RayHit hit = entity.Intersect(ray);

                if (hit != null)
                {
                    var distance = hit.Position.distanceBetween(rayOrigin);
                    if (distance < tNear)
                    {
                        tNear = distance;
                        closestEntity = entity;
                        closestHitRay = hit;
                    }
                }
            }

            if (closestEntity != null)
            {
                // We only consider the closest entity to the camera
                Double bias = 1e-4;
                Color outputColor = new Color(0, 0, 0);

                switch (closestEntity.Material.Type)
                {
                    case (Material.MaterialType.Diffuse):
                        outputColor += diffuseCalc(closestHitRay, bias);
                        break;

                    case (Material.MaterialType.Reflective):
                        Vector3 reflectDir = reflect(closestHitRay.Incident, closestHitRay.Normal).Normalized();
                        outputColor += castRay(closestHitRay.Position + closestHitRay.Normal * bias, reflectDir, depth + 1);
                        break;

                    case (Material.MaterialType.Glossy):

                        // Determines how glossy an object is [1 for fully reflective, 0 for fully diffuse]
                        float glossyFactor = 0.2f;

                        Color glossyComponent = new Color(0, 0, 0);
                        Color diffuseComponent = new Color(0, 0, 0);
                        Vector3 glossyDir = reflect(closestHitRay.Incident, closestHitRay.Normal).Normalized();

                        glossyComponent += castRay(closestHitRay.Position + closestHitRay.Normal * bias, glossyDir, depth + 1);
                        diffuseComponent += diffuseCalc(closestHitRay, bias);

                        // Glossiness is the ratio of the relective component to the diffuse component
                        outputColor += glossyComponent * glossyFactor + diffuseComponent * (1 - glossyFactor);

                        break;

                    case (Material.MaterialType.Refractive):

                        Color refractionColor = new Color(0, 0, 0);
                        Vector3 dir = closestHitRay.Incident;
                        Vector3 hitNormal = closestHitRay.Normal;
                        Vector3 hitPoint = closestHitRay.Position;
                        double ior = closestEntity.Material.RefractiveIndex;

                        double kr = fresnel(dir, hitNormal, ior);
                        bool outside = dir.Dot(hitNormal) < 0;
                        Vector3 rbias = bias * hitNormal;
                        // compute refraction if it is not a case of total internal reflection
                        if (kr < 1)
                        {
                            Vector3 refractionDirection = refract(dir, hitNormal, ior).Normalized();
                            Vector3 refractionRayOrig = outside ? hitPoint - rbias : hitPoint + rbias;

                            // Beer's Law
                            Ray innerRay = new Ray(refractionRayOrig, refractionDirection);
                            RayHit innerHit = closestEntity.Intersect(innerRay);

                            refractionColor = castRay(refractionRayOrig, refractionDirection, depth + 1);

                            if (innerHit != null)
                            {
                                // Beers Law - The material absorbs a particular wavelength of light
                                double absorbDistance = innerHit.Position.distanceBetween(refractionRayOrig);
                                Color absorbance = closestEntity.Material.Color;
                                Color transparency = new Color(Math.Exp(absorbance.R), Math.Exp(absorbance.G), Math.Exp(absorbance.B));
                                refractionColor *= transparency;
                            }
                        }

                        Vector3 reflectionDirection = reflect(dir, hitNormal).Normalized();
                        Vector3 reflectionRayOrig = outside ? hitPoint + rbias : hitPoint - rbias;
                        Color reflectionColor = castRay(reflectionRayOrig, reflectionDirection, depth + 1);

                        // mix the two
                        outputColor += reflectionColor * kr + refractionColor * (1 - kr);
                        break;

                }

                return outputColor;
            }

            return new Color(0, 0, 0);
        }

        public Color diffuseCalc(RayHit closestHitRay, double bias)
        {
            Color outputColor = new Color(0, 0, 0);
            foreach (PointLight light in this.lights)
            {
                var visible = 1;
                // Fire another ray towards each light source from the closest entity
                var rayToLight = new Ray(closestHitRay.Position + closestHitRay.Normal * bias, light.getDirection(closestHitRay.Position));
                double disToLight = closestHitRay.Position.distanceBetween(light.Position);


                foreach (SceneEntity shadowEntity in this.entities)
                {
                    RayHit checkBlock = shadowEntity.Intersect(rayToLight);

                    if (checkBlock != null)
                    {
                        double disToObj = (closestHitRay.Position).distanceBetween(checkBlock.Position);

                        if (disToObj < disToLight)
                        {
                            visible = 0;
                            break;
                        }
                    }

                }
                outputColor += diffuseColor(closestHitRay, light) * visible;
            }
            return outputColor;
        }

        public Color diffuseColor(RayHit hitInfo, PointLight light)
        {
            Vector3 hitNormal = hitInfo.Normal;
            Vector3 dirToLight = (light.Position - hitInfo.Position).Normalized();
            Color materialColor = hitInfo.Material.Color;
            Color lightColor = light.Color;

            Color outputColor = materialColor * lightColor * Math.Max(0, hitNormal.Dot(dirToLight));

            return outputColor;
        }

        Vector3 reflect(Vector3 incident, Vector3 normal)
        {
            return incident - 2 * incident.Dot(normal) * normal;
        }

        Vector3 refract(Vector3 incident, Vector3 normal, double ior)
        {
            double cosi = clamp(-1, 1, incident.Dot(normal));
            double etai = 1, etat = ior;
            Vector3 n = normal;

            if (cosi < 0)
            {
                cosi = -cosi;
            }
            else
            {
                double tmp = etai;
                etai = etat;
                etat = tmp;
                n = -normal;
            }

            double eta = etai / etat;
            double k = 1 - eta * eta * (1 - cosi * cosi);
            return k < 0 ? new Vector3(0, 0, 0) : eta * incident + (eta * cosi - Math.Sqrt(k)) * n;
        }

        double fresnel(Vector3 incident, Vector3 normal, double ior)
        {
            double cosi = clamp(-1, 1, incident.Dot(normal));
            double etai = 1, etat = ior;
            if (cosi > 0)
            {
                double tmp = etai;
                etai = etat;
                etat = tmp;
            }
            // Compute sini using Snell's law
            double sint = etai / etat * Math.Sqrt(Math.Max(0, 1 - cosi * cosi));
            // Total internal reflection
            if (sint >= 1)
            {
                return 1;
            }
            else
            {
                double cost = Math.Sqrt(Math.Max(0, 1 - sint * sint));
                cosi = Math.Abs(cosi);
                double Rs = ((etat * cosi) - (etai * cost)) / ((etat * cosi) + (etai * cost));
                double Rp = ((etai * cosi) - (etat * cost)) / ((etai * cosi) + (etat * cost));
                return (Rs * Rs + Rp * Rp) / 2;
            }
            // As a consequence of the conservation of energy, transmittance is given by:
            // kt = 1 - kr;
        }

        double clamp(double lo, double hi, double v)
        {
            return Math.Max(lo, Math.Min(hi, v));
        }

    }



}
