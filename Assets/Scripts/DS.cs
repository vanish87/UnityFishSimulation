using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.Common;
using UnityTools.Debuging.EditorTool;
using UnityTools;
using UnityTools.Math;
using static UnityFishSimulation.FishSAOptimizer.FishSA;
using System;
using UnityTools.Debuging;

namespace UnityFishSimulation
{
    public class DS : MonoBehaviour
    {

        public class Problem : DownhillSimplex<float>.Problem
        {
            float a = 1;
            float b = 100;
            float scale = 1;

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

        public class Problem2 : DownhillSimplex<float>.Problem
        {
            public Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations = new Dictionary<Spring.Type, X2FDiscreteFunction<float>>();

            public float2 timeInterval = new float2(0, 20);
            public int sampleNum = 15;

            public Problem2() : this(0)
            {
            }
            public Problem2(int dim) : base(dim)
            {
                var start = new Tuple<float, float>(this.timeInterval.x, 0.5f);
                var end = new Tuple<float, float>(this.timeInterval.y, 0.5f);

                //this.activations.Add(Spring.Type.MuscleFront, new X2FDiscreteFunction<float>(start, end, this.sampleSize));
                this.activations.Add(Spring.Type.MuscleMiddle, new X2FDiscreteFunction<float>(start, end, this.sampleNum));
                this.activations.Add(Spring.Type.MuscleBack, new X2FDiscreteFunction<float>(start, end, this.sampleNum));

                foreach(var fun in this.activations.Values)
                {
                    fun.RandomValues();
                }

                this.dim = this.activations.Count * this.sampleNum;
            }

            public override float Evaluate(Vector<float> v)
            {
                
                var temp = this.activations.DeepCopy();

                var kcount = 0;
                foreach (var k in temp.Keys)
                {
                    for (var i = 0; i < this.sampleNum; ++i)
                    {
                        temp[k][i] = v[i + kcount * this.sampleNum];
                    }
                    kcount++;
                }

                var mu1 = 1f;
                var mu2 = 1f;

                var v1 = 0.0f;//actuation amplitudes
                var v2 = 0.002f;//actuation variation

                var E = 0f;


                /*var fp = new FishSimulator.Problem(FishSimulator.Problem.SolverType.Eular, temp);
                var delta = new FishSimulator.Delta();

                var simulator = new FishSimulator(fp, delta);
                simulator.StartSimulation();
                //Debug.Log("Start");
                while (simulator.IsSimulationDone() == false) { }
                simulator.Stop();
                //Debug.Log("Done");

                var sol = simulator.CurrentSolution as FishSimulator.Solution;

                var trajactory = sol?.trajactory;
                var velocity = sol?.velocity;*/

                for (int i = 0; i < this.sampleNum; ++i)
                {
                    var Eu = 0f;
                    //var Ev = math.length(trajactory.Evaluate(i) - new float3(100, 0, 0)) - velocity.Evaluate(i).x;
                    var Ev = 0;


                    var du = 0f;
                    var du2 = 0f;
                    foreach (var fun in temp.Values)
                    {
                        var dev = fun.Devrivate(i);
                        var dev2 = fun.Devrivate2(i);
                        du += dev * dev;
                        du2 += dev2 * dev2;
                    }

                    Eu = 0.5f * (v1 * du + v2 * du2);

                    E += mu1 * Eu + mu2 * Ev;
                }

                return E;
            }

            public override Vector<float> Generate(Vector<float> x)
            {
                if (x == null) x = new Vector<float>(this.dim);

                for(var i = 0; i < x.Size; ++i)
                {
                    x[i] = ThreadSafeRandom.NextFloat();
                }

                return x;
            }
        }

        public class Delta : IDelta
        {
            public void Reset()
            {
            }

            public void Step()
            {
            }
        }

        protected DownhillSimplex<float> simplex;
        protected DownhillSimplex<float>.Problem p;
        protected DownhillSimplex<float>.Solution sol;

        protected FishSimulator simulator;
        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();
        protected void Start()
        {
            p = new Problem2();
            var d = new Delta();
            this.simplex = new DownhillSimplex<float>(p, d);
            this.simplex.ChangeState(this.simplex.Running);

            /*var meshFiter = this.gameObject.FindOrAddTypeInComponentsAndChilden<MeshFilter>();
            var meshRender = this.gameObject.FindOrAddTypeInComponentsAndChilden<MeshRenderer>();
            meshRender.material = new Material(Shader.Find("Diffuse"));

            var mesh = FunctionTool.GenerateFunctionMesh(p);
            meshFiter.mesh = mesh;*/

            var p2 = this.p as Problem2;
            if (p2 != null)
            {
                var fp = new FishSimulator.Problem(p2.activations);
                var delta = new FishSimulator.Delta();

                this.simulator = new FishSimulator(FishSimulator.SolverType.Euler, fp, delta);
                this.simulator.StartSimulation();
            }
        }

        protected void OnDestroy()
        {
            /*var meshRender = this.gameObject.FindOrAddTypeInComponentsAndChilden<MeshRenderer>();
            var mat = meshRender.material;
            mat.DestoryObj();*/
        }


        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                this.sol = this.simplex.Solve(p) as DownhillSimplex<float>.Solution;
                this.sol.min.Print();                
            }

            if (this.simplex.IsRunning() == false)
            {
                this.sol = this.simplex.CurrentSolution as DownhillSimplex<float>.Solution;
                this.sol.min.Print();
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                this.sol = this.simplex.CurrentSolution as DownhillSimplex<float>.Solution;

                var p2 = this.p as Problem2;
                var kcount = 0;
                var x = this.sol.min.X;

                foreach (var k in p2.activations.Keys)
                {
                    for (var i = 0; i < p2.sampleNum; ++i)
                    {
                        p2.activations[k][i] = x[i + kcount * p2.sampleNum];
                    }
                    kcount++;
                }

                if (this.simulator.IsSimulationDone()) this.simulator.StartSimulation();


                this.curves.Clear();

                foreach (var d in p2.activations.Values)
                {
                    this.curves.Add(d.ToAnimationCurve());
                }
            }

            if(Input.GetKeyDown(KeyCode.R))
            {
                var p2 = this.p as Problem2;

                foreach(var v in p2.activations.Values)
                {
                    v.RandomValues();
                }

                this.curves.Clear();

                foreach (var d in p2.activations.Values)
                {
                    this.curves.Add(d.ToAnimationCurve());
                }
            }

            if(Input.GetKeyDown(KeyCode.D))
            {
                var p2 = this.p as Problem2;
                var temp = p2.activations.DeepCopy();
                var kcount = 0;
                var x = this.sol.min.X;

                foreach (var k in temp.Keys)
                {
                    for (var i = 0; i < p2.sampleNum; ++i)
                    {
                        temp[k][i] = x[i + kcount * p2.sampleNum];
                    }
                    kcount++;
                }

                this.Save(temp, "DSSwimming");
            }
        }

        protected void Save(Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations, string file = "Swimming")
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, file + ".func");
            FileTool.Write(path, activations);
            LogTool.Log("Saved " + path);
        }

        protected void OnDisable()
        {
            this.simplex.Stop();
            this.simulator.Stop();
        }

        protected void OnDrawGizmos()
        {
            if (p == null ) return;
            /*var n = 50;
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
            }*/
            
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


            this.simulator?.OnGizmos();
        }
    }
}
