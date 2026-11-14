using UnityEngine;

namespace Game.Entities.Environment
{
    /// <summary>
    /// Builds a low-poly biconcave disc (the classic red blood cell shape), 1 unit in diameter.
    /// </summary>
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
                for (int r = 0; r <= radialSteps; r++)
                {
                    float rho = (float)r / radialSteps;
                    float height = HalfThickness(rho) * sign;
                    for (int a = 0; a <= angularSteps; a++)
                    {
                        float angle = (float)a / angularSteps * Mathf.PI * 2f;
                        vertices[side * surfaceVertices + r * ringVertices + a] =
                            new Vector3(Mathf.Cos(angle) * rho * 0.5f, height, Mathf.Sin(angle) * rho * 0.5f);
                    }
                }
            }

            int tri = 0;
            for (int side = 0; side < 2; side++)
            {
                int offset = side * surfaceVertices;
                for (int r = 0; r < radialSteps; r++)
                {
                    for (int a = 0; a < angularSteps; a++)
                    {
                        int i0 = offset + r * ringVertices + a;
                        int i1 = i0 + 1;
                        int i2 = i0 + ringVertices;
                        int i3 = i2 + 1;
                        if (side == 0)
                        {
                            triangles[tri++] = i0; triangles[tri++] = i2; triangles[tri++] = i1;
                            triangles[tri++] = i1; triangles[tri++] = i2; triangles[tri++] = i3;
                        }
                        else
                        {
                            triangles[tri++] = i0; triangles[tri++] = i1; triangles[tri++] = i2;
                            triangles[tri++] = i1; triangles[tri++] = i3; triangles[tri++] = i2;
                        }
                    }
                }
            }

            var mesh = new Mesh { name = "Blood Cell", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Evans–Fung profile normalised by the cell radius: thin dimple in the middle, thick rounded rim
        // (about 5% of the diameter at the centre, about 33% at the rim).
        private static float HalfThickness(float rho)
        {
            float rho2 = rho * rho;
            float shape = Mathf.Sqrt(Mathf.Max(0f, 1f - rho2)) * (0.207f + 2.003f * rho2 - 1.123f * rho2 * rho2);
            return Mathf.Max(0.004f, shape * 0.25f);
        }
    }
}
