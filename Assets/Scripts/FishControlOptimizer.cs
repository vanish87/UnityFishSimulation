using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Math;

namespace UnityFishSimulation
{
    public class FishActivationData
    {
        public static Dictionary<Spring.Type, X2FDiscreteFunction<float>> VectorToActivation(Type type, Vector<float> x, float2 interval, int sampleNum)
        {
            var activations = new Dictionary<Spring.Type, X2FDiscreteFunction<float>>();

            switch (type)
            {
                case Type.Swimming:
                    {
                        activations.Add(Spring.Type.MuscleMiddle, new X2FDiscreteFunction<float>(interval.x, interval.y, Vector<float>.Sub(0, sampleNum, x)));
                        activations.Add(Spring.Type.MuscleBack, new X2FDiscreteFunction<float>(interval.x, interval.y, Vector<float>.Sub(sampleNum, sampleNum * 2, x)));
                    }
                    break;
                default: break;
            }

            return activations;
        }

        public enum Type
        {
            Swimming,
        }
        protected float2 interval;
        protected int sampleNum;
        protected Type type = Type.Swimming;

        protected Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations;

        public Dictionary<Spring.Type, X2FDiscreteFunction<float>> Activations { get => this.activations; }

        public FishActivationData(float2 interval, int sampleNum = 15, Type type = Type.Swimming)
        {
            this.interval = interval;
            this.sampleNum = sampleNum;
            this.type = type;

            this.activations = new Dictionary<Spring.Type, X2FDiscreteFunction<float>>();
            switch (this.type)
            {
                case Type.Swimming:
                    {
                        var start = new Tuple<float, float>(interval.x, 0);
                        var end = new Tuple<float, float>(interval.y, 0);
                        activations.Add(Spring.Type.MuscleMiddle, new X2FDiscreteFunction<float>(start, end, this.sampleNum));
                        activations.Add(Spring.Type.MuscleBack, new X2FDiscreteFunction<float>(start, end, this.sampleNum));
                    }
                    break;
                default: break;
            }
        }

        public void UpdateFromVector(Vector<float> x)
        {
            this.activations = VectorToActivation(this.type, x, this.interval, this.sampleNum);
        }
    }
    public class FishControlOptimizer : MonoBehaviour
    {
        public class Problem : DownhillSimplex<float>.Problem
        {
            protected FishActivationData fishActivationData;
            protected float2 interval;
            protected int sampleNum;
            public Problem(float2 interval, int sampleNum) : base(0)
            {
                this.interval = interval;
                this.sampleNum = sampleNum;

                this.fishActivationData = new FishActivationData(this.interval, this.sampleNum);
                this.dim = this.fishActivationData.Activations.Count * this.sampleNum;
            }

            public override float Evaluate(Vector<float> x)
            {
                //from vector x
                //convert to X2FDiscreteFunction
                this.fishActivationData.UpdateFromVector(x);

                var useSim = true;
                FishSimulator simulator = null;
                if (useSim)
                {
                    var problem = new FishSimulator.Problem(this.fishActivationData.Activations);
                    var delta = new FishSimulator.Delta();


                    simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta);
                    simulator.StartSimulation();

                    //Debug.Log("start");
                    //start new simulation to get trajactory
                    //wait to finish
                    while (simulator.IsSimulationDone() == false) { }
                }

                var e = this.GetCurrentE(simulator?.CurrentSolution as FishSimulator.Solution, this.fishActivationData.Activations, this.sampleNum);

                if(useSim) simulator.StopThread();

                Debug.Log("end with e = " + e);
                //cal new E from RandomX2FDiscreteFunction and trajactory

                return e;
            }

            public override Vector<float> Generate(Vector<float> x)
            {
                if (x == null) x = new Vector<float>(this.dim);

                for (var i = 0; i < x.Size; ++i)
                {
                    x[i] = ThreadSafeRandom.NextFloat();
                }

                return x;
            }

            protected float GetCurrentE(FishSimulator.Solution sol, Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations, int sampleSize)
            {
                var mu1 = 0.5f;
                var mu2 = 1f;

                var v1 = 0.5f;
                var v2 = 0.05f;

                var E = 0f;

                var useSol = sol != null;

                var trajactory = sol?.trajactory;
                var velocity = sol?.velocity;

                var Ev = useSol?math.length(trajactory.End.Item2 - new float3(100, 0, 0)):0;

                for (int i = 0; i < sampleSize; ++i)
                {
                    var Eu = 0f;
                    Ev += useSol ? -velocity[i].x : 0;
                    //var Ev = 0;


                    var du = 0f;
                    var du2 = 0f;
                    foreach (var fun in activations.Values)
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

        [SerializeField] protected float2 timeInterval = new float2(0, 20);
        [SerializeField] protected int sampleNum = 15;

        protected DownhillSimplex<float> simplex;
        protected DownhillSimplex<float>.Problem p;
        protected DownhillSimplex<float>.Solution sol;


        protected FishSimulator simulator;

        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();

        protected void Start()
        {
            p = new Problem(this.timeInterval, this.sampleNum);
            var d = new Delta();
            this.simplex = new DownhillSimplex<float>(p, d);
            this.simplex.TryToRun();

            this.simplex.PerStep((p, s, dt, a) =>
            {
                LogTool.Log("Solution Updated: ", LogLevel.Info);
                var sol = s as DownhillSimplex<float>.Solution;
                sol.min.Print();
            });
        }
        
        protected void Update()
        {
            if(Input.GetKeyDown(KeyCode.U))
            {
                var sol = (this.simplex.CurrentSolution) as DownhillSimplex<float>.Solution;
                var activations = FishActivationData.VectorToActivation(FishActivationData.Type.Swimming, sol.min.X, this.timeInterval, this.sampleNum);

                var problem = new FishSimulator.Problem(activations);
                var delta = new FishSimulator.Delta();

                if(this.simulator != null)this.simulator.StopThread();
                this.simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta);
                this.simulator.StartSimulation();

                this.curves.Clear();
                foreach(var act in activations.Values)
                {
                    this.curves.Add(act.ToAnimationCurve());
                }
            }
        }

        protected void OnDrawGizmos()
        {
            this.simulator?.OnGizmos();
        }


    }
}