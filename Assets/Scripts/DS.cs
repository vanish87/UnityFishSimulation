using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.Common;
using UnityTools.Debuging.EditorTool;

namespace UnityFishSimulation
{
    public class DS : MonoBehaviour
    {

        public class Problem : DownhillSimplex<float>.Problem
        {
            float a = 1;
            float b = 100;
            float scale = 0.02f;

            public Problem(int dim = 2) : base(dim)
            {
            }
            public override float Evaluate(Vector<float> v)
            {
                var x = v[0] * scale;
                var y = v[1] * scale;

                var by = (y - x * x);
                var ret = (a - x) * (a - x) + b * by * by;
                return ret;
            }

            public override Vector<float> Generate(Vector<float> x)
            {
                var p = new Vector<float>(2);
                p[0] = ThreadSafeRandom.NextFloat() * 50;
                p[1] = ThreadSafeRandom.NextFloat() * 50;
                return p;
            }
        }

        public class Problem1 : DownhillSimplex<float>.Problem
        {
            public Problem1(int dim = 2) : base(dim)
            {
            }

            public override float Evaluate(Vector<float> v)
            {
                var x = v[0];
                var y = v[1];

                var v1 = x * x + y - 11;
                var v2 = x + y * y - 7;
                return v1 * v1 + v2 * v2;
            }

            public override Vector<float> Generate(Vector<float> x)
            {
                var p = new Vector<float>(2);
                p[0] = ThreadSafeRandom.NextFloat() * 50;
                p[1] = ThreadSafeRandom.NextFloat() * 50;
                return p;
            }
        }

        public class Delta : IDelta
        {
            public const float dt = 0.055f;
            public float current;
            public void Reset()
            {
                this.current = 0;
            }

            public void Step()
            {
                this.current += dt;
            }
        }

        protected DownhillSimplex<float> simplex;
        protected DownhillSimplex<float>.Problem p;
        protected DownhillSimplex<float>.Solution sol;
        protected void Start()
        {
            p = new Problem();
            var d = new Delta();
            this.simplex = new DownhillSimplex<float>(p, d);
            //this.simplex.ChangeState(this.simplex.Running);
        }


        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                this.sol = this.simplex.Solve(p) as DownhillSimplex<float>.Solution;
                this.sol.min.Print();                
            }
        }
        protected void OnDisable()
        {
            this.simplex.Stop();
        }

        protected void OnDrawGizmos()
        {
            if (p == null ) return;
            var n = 50;
            for(var i = 0; i < n; ++i)
            {
                for(var j = 0; j < n; ++j)
                {
                    var vp = new Vector<float>(p.dim);
                    vp[0] = i;
                    vp[1] = j;
                    var point = new float3(i, this.p.Evaluate(vp), j);
                    Gizmos.DrawSphere(point, 0.8f);

                }
            }
            
            var simplex = this.simplex.Vertices;
            if (simplex != null)
            {
                using (new GizmosScope(Color.blue, Matrix4x4.identity))
                {
                    var p1 = new Vector3(simplex[0].X[0], simplex[0].Fx, simplex[0].X[1]);
                    var p2 = new Vector3(simplex[1].X[0], simplex[1].Fx, simplex[1].X[1]);
                    var p3 = new Vector3(simplex[2].X[0], simplex[2].Fx, simplex[2].X[1]);
                    Gizmos.DrawLine(p1, p2);
                    Gizmos.DrawLine(p1, p3);
                    Gizmos.DrawLine(p2, p3);
                }
            }
        }
    }
}
