using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.Common;
using UnityTools.Math;

namespace UnityFishSimulation
{
    public class FishControlOptimizer : MonoBehaviour
    {
        protected static Dictionary<Spring.Type, X2FDiscreteFunction<float>> VectorToActivation(Vector<float> x, float2 interval, int sampleNum)
        {
            var activations = new Dictionary<Spring.Type, X2FDiscreteFunction<float>>();
            var start = new Tuple<float, float>(interval.x, x[0]);
            var end = new Tuple<float, float>(interval.y, x[x.Size - 1]);

            activations.Add(Spring.Type.MuscleMiddle, new X2FDiscreteFunction<float>(start, end, Vector<float>.Sub(0, sampleNum, x)));
            activations.Add(Spring.Type.MuscleBack, new X2FDiscreteFunction<float>(start, end, Vector<float>.Sub(sampleNum, sampleNum * 2, x)));

            return activations;
        }
        public class Problem : DownhillSimplex<float>.Problem
        {
            protected float2 interval;
            protected int sampleNum;
            public Problem(int dim, float2 interval, int sampleNum) : base(dim)
            {
                this.interval = interval;
                this.sampleNum = sampleNum;
            }

            public override float Evaluate(Vector<float> x)
            {
                //from vector x
                //convert to X2FDiscreteFunction
                var activations = VectorToActivation(x, this.interval, this.sampleNum);

                var useSim = true;
                FishSimulator simulator = null;
                if (useSim)
                {
                    var problem = new FishSimulator.Problem(activations);
                    var delta = new FishSimulator.Delta();


                    simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta);
                    simulator.StartSimulation();

                    //Debug.Log("start");
                    //start new simulation to get trajactory
                    //wait to finish
                    while (simulator.IsSimulationDone() == false) { }
                }

                var e = this.GetCurrentE(simulator?.CurrentSolution as FishSimulator.Solution, activations, this.sampleNum);

                if(useSim) simulator.Stop();

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

        protected Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations = new Dictionary<Spring.Type, X2FDiscreteFunction<float>>();
        protected DownhillSimplex<float> simplex;
        protected DownhillSimplex<float>.Problem p;
        protected DownhillSimplex<float>.Solution sol;


        protected FishSimulator simulator;

        protected void Start()
        {
            this.InitActivations();

            var dim = this.activations.Count * this.sampleNum;

            p = new Problem(dim, this.timeInterval, this.sampleNum);
            var d = new Delta();
            this.simplex = new DownhillSimplex<float>(p, d);
            this.simplex.ChangeState(this.simplex.Running);
        }

        protected void OnDisable()
        {
            this.simplex.ChangeState(this.simplex.Done);
            this.simplex.Stop();
            this.simulator?.Stop();
        }

        protected void Update()
        {
            if(Input.GetKeyDown(KeyCode.U))
            {
                var sol = (this.simplex.CurrentSolution) as DownhillSimplex<float>.Solution;
                this.activations = VectorToActivation(sol.min.X, this.timeInterval, this.sampleNum);

                var problem = new FishSimulator.Problem(this.activations);
                var delta = new FishSimulator.Delta();

                if(this.simulator != null)this.simulator.Stop();
                this.simulator = new FishSimulator(FishSimulator.SolverType.Euler, problem, delta);
                this.simulator.StartSimulation();
            }
        }

        protected void OnDrawGizmos()
        {
            this.simulator?.OnGizmos();
        }

        protected void InitActivations()
        {
            this.activations.Clear();

            var start = new Tuple<float, float>(this.timeInterval.x, 0.5f);
            var end = new Tuple<float, float>(this.timeInterval.y, 0.5f);

            //this.activations.Add(Spring.Type.MuscleFront, new X2FDiscreteFunction<float>(start, end, this.sampleSize));
            this.activations.Add(Spring.Type.MuscleMiddle, new X2FDiscreteFunction<float>(start, end, this.sampleNum));
            this.activations.Add(Spring.Type.MuscleBack, new X2FDiscreteFunction<float>(start, end, this.sampleNum));

            foreach (var fun in this.activations.Values)
            {
                fun.RandomValues();
            }
        }

    }
}