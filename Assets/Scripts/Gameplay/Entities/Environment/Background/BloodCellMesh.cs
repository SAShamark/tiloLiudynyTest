using UnityEngine;

namespace Gameplay.Entities.Environment.Background
{
    public static class BloodCellMesh
    {
        public static Mesh Create(int radialSteps = 8, int angularSteps = 16)
        {
            int ringVertices = angularSteps + 1;
            int surfaceVertices = (radialSteps + 1) * ringVertices;
            var vertices = new Vector3[surfaceVertices * 2];
            var triangles = new int[radialSteps * angularSteps * 6 * 2];

            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? 1f : -1f;
                for (int ring = 0; ring <= radialSteps; ring++)
                {
                    float radialFraction = (float)ring / radialSteps;
                    float height = HalfThickness(radialFraction) * sign;
                    for (int segment = 0; segment <= angularSteps; segment++)
                    {
                        float angle = (float)segment / angularSteps * Mathf.PI * 2f;
                        vertices[side * surfaceVertices + ring * ringVertices + segment] =
                            new Vector3(Mathf.Cos(angle) * radialFraction * 0.5f, height, Mathf.Sin(angle) * radialFraction * 0.5f);
                    }
                }
            }

            int triangleIndex = 0;
            for (int side = 0; side < 2; side++)
            {
                int offset = side * surfaceVertices;
                for (int ring = 0; ring < radialSteps; ring++)
                {
                    for (int segment = 0; segment < angularSteps; segment++)
                    {
                        int inner = offset + ring * ringVertices + segment;
                        int innerNext = inner + 1;
                        int outer = inner + ringVertices;
                        int outerNext = outer + 1;
                        if (side == 0)
                        {
                            triangles[triangleIndex++] = inner; triangles[triangleIndex++] = outer; triangles[triangleIndex++] = innerNext;
                            triangles[triangleIndex++] = innerNext; triangles[triangleIndex++] = outer; triangles[triangleIndex++] = outerNext;
                        }
                        else
                        {
                            triangles[triangleIndex++] = inner; triangles[triangleIndex++] = innerNext; triangles[triangleIndex++] = outer;
                            triangles[triangleIndex++] = innerNext; triangles[triangleIndex++] = outerNext; triangles[triangleIndex++] = outer;
                        }
                    }
                }
            }

            var mesh = new Mesh { name = "Blood Cell", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float HalfThickness(float radialFraction)
        {
            float rho2 = radialFraction * radialFraction;
            float shape = Mathf.Sqrt(Mathf.Max(0f, 1f - rho2)) * (0.207f + 2.003f * rho2 - 1.123f * rho2 * rho2);
            return Mathf.Max(0.004f, shape * 0.25f);
        }
    }
}
