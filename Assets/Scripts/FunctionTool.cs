using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Common;

namespace UnityTools.Math
{
    public interface Function<X, Y>
    {
        Y Evaluate(X x);
        X Generate(X x);
    }
    public static class FunctionTool
    {
        public static Mesh GenerateFunctionMesh(Function<Vector<float>, float> func)
        {
            var mesh = new Mesh();
            var n = 50;
            var xSize = n;
            var ySize = n;


            var vertices = new Vector3[(xSize + 1) * (ySize + 1)];
            for (int i = 0, y = 0; y <= ySize; y++)
            {
                for (int x = 0; x <= xSize; x++, i++)
                {
                    var pv = new Vector<float>(2);
                    pv[0] = x;
                    pv[1] = y;
                    var py = func.Evaluate(pv);
                    vertices[i] = new Vector3(pv[0], py, pv[1]);
                }
            }
            mesh.vertices = vertices;

            var triangles = new int[xSize * ySize * 6];
            for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
            {
                for (int x = 0; x < xSize; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                    triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
                    triangles[ti + 5] = vi + xSize + 2;
                }
            }
            mesh.triangles = triangles;

            mesh.RecalculateNormals();

            return mesh;
        }
    }
}