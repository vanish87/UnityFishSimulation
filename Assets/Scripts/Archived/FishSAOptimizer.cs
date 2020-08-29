using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Algorithm;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Debuging.EditorTool;
using UnityTools.Math;

namespace UnityFishSimulation
{
    public class FishSAOptimizer : MonoBehaviour
    {
        [SerializeField] protected List<AnimationCurve> curves = new List<AnimationCurve>();
       /* public class SwimmingProblem : Problem
        {
            public SwimmingProblem(float from, float to, int sampleNum, SolverType type) : base(from, to, type)
            {
                var start = new Tuple<float, float>(from, 0.5f);
                var end = new Tuple<float, float>(to, 0.5f);
                this.activations = new Dictionary<Spring.Type, MotorController.RandomX2FDiscreteFunction>()
                {
                    {Spring.Type.MuscleMiddle, new MotorController.RandomX2FDiscreteFunction(start, end, sampleNum)},
                    {Spring.Type.MuscleBack, new MotorController.RandomX2FDiscreteFunction(start, end, sampleNum)},
                };
            }
        }*/
        public class FishSA : SimulatedAnnealing
        {
            /*[System.Serializable]
            public class RandomX2FDiscreteFunction : X2FDiscreteFunction<float>
            {
                public RandomX2FDiscreteFunction(AnimationCurve from) : base(from) { }
                public RandomX2FDiscreteFunction(Tuple<float, float> start, Tuple<float, float> end, int sampleNum) : base(start, end, sampleNum)
                {

                }

                public void RandomCurrentValues()
                {
                    var current = this.valueMap;
                    for (var i = 0; i < current.Count; ++i)
                    {
                        var n = current[i].Item1;
                        var y = current[i].Item2;
                        current[i] = new Tuple<float, float>(n, ThreadSafeRandom.NextFloat());
                    }
                }
                public override void RandomValues()
                {
                    var range = 1;
                    for (var i = 0; i < this.valueMap.Count; ++i)
                    {
                        var n = this.valueMap[i].Item1;
                        var y = this.valueMap[i].Item2;
                        this.valueMap[i] = new Tuple<float, float>(n, math.saturate(y + (ThreadSafeRandom.NextFloat() - 0.5f) * 2 * range));
                    }
                }
            }*/

            [System.Serializable]
            public class FishStateData: IState
            {
                public Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations = new Dictionary<Spring.Type, X2FDiscreteFunction<float>>();
                [NonSerialized] public FishSimulator simulator;
                [NonSerialized] public FishSimulator.Problem problem;

                public float2 timeInterval = new float2(0, 50);
                public int sampleSize = 30;


                public int GetVectorDim() { return this.sampleSize * this.activations.Count; }
                public Vector<float> GetVector()
                {
                    var ret = new Vector<float>(this.sampleSize * this.activations.Count);
                    var count = 0;
                    foreach (var v in this.activations.Values)
                    {
                        for (var s = 0; s < v.SampleNum; ++s)
                        {
                            ret[count++] = v[s];
                        }
                    }

                    return ret;
                }
                public void SetVector(Vector<float> vector)
                {
                    var count = 0;
                    foreach (var v in this.activations.Values)
                    {
                        for (var s = 0; s < v.SampleNum; ++s)
                        {
                            v[s] = vector[count++];
                        }
                    }
                }

                public float E
                {
                    get
                    {
                        var sol = this.simulator.CurrentSolution as FishSimulator.Solution;
                        return GetCurrentE(sol, this.activations, this.sampleSize);
                    }
                }

                public void UpdateNewValue()
                {
                    foreach(var act in this.activations)
                    {
                        act.Value.RandomValues();
                    }
                }

                public float Evaluate(IState x)
                {
                    throw new NotImplementedException();
                }

                public IState Generate(IState x)
                {
                    throw new NotImplementedException();
                }

                public FishStateData()
                {
                    var start = new Tuple<float, float>(this.timeInterval.x, 0.5f);
                    var end = new Tuple<float, float>(this.timeInterval.y, 0.5f);

                    //this.activations.Add(Spring.Type.MuscleFront, new X2FDiscreteFunction<float>(start, end, this.sampleSize));
                    this.activations.Add(Spring.Type.MuscleMiddle, new X2FDiscreteFunction<float>(start, end, this.sampleSize));
                    this.activations.Add(Spring.Type.MuscleBack, new X2FDiscreteFunction<float>(start, end, this.sampleSize));

                    problem = new FishSimulator.Problem(FishActivationData.Type.Swimming);
                    var delta = new FishSimulator.Delta();

                    this.simulator = new FishSimulator(FishSimulator.SolverType.Euler,problem, delta);
                }
            }
            public class FishState : CricleData<FishStateData, int>
            {
                public FishState() : base(2)
                {

                }

                protected override FishStateData OnCreate(int para)
                {
                    return new FishStateData();
                }
            }
            public class FishProblem : Problem
            {
                protected FishState state = new FishState();

                public override IState Current => this.state.Current;

                public override IState Next => this.state.Next;


                public override void MoveToNext()
                {
                    this.state.MoveToNext();
                }

                public float3 targetPoint;
            }
            public class FishSASolution
            {
                public Dictionary<Spring.Type, X2FDiscreteFunction<float>> actvations;
            }


            public static float GetCurrentE(FishSimulator.Solution sol, Dictionary<Spring.Type, X2FDiscreteFunction<float>> activations, int sampleSize)
            {
                var mu1 = 1f;
                var mu2 = 0f;

                var v1 = 0.01f;
                var v2 = 1f;

                var E = 0f;

                //var trajactory = sol?.trajactory;
                //var velocity = sol?.velocity;

                for (int i = 0; i < sampleSize; ++i)
                {
                    var Eu = 0f;
                    //var Ev = math.length(trajactory.Evaluate(i) - new float3(100, 0, 0)) - velocity.Evaluate(i).x;
                    var Ev = 0;


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

            public FishSA(IProblem problem, IDelta dt) : base(problem, dt)
            {

            }

            public void StartSA()
            {
                var p = this.problem as FishProblem;

                var current = p.Current as FishStateData;
                var next = p.Next as FishStateData;

                current.simulator.TryToRun();
                next.simulator.TryToRun();

                this.ChangeState(this.Running);
            }

            public class Vertice
            {
                public float E;
                public FishStateData state;
            }

            protected List<Vertice> simplex = new List<Vertice>();

            public override ISolution Solve(IProblem problem)
            {
                LogTool.LogAssertIsTrue(this.dt != null, "Dt is null");
                var sol = new Solution();
                var p = this.problem as FishProblem;

                var current = p.Current as FishStateData;
                var next = p.Next as FishStateData;

                if (current.simulator.IsSimulationDone() 
                    && next.simulator.IsSimulationDone())
                { 

                    if (this.ShouldUseNext(current.E, next.E))
                    {
                        LogTool.Log("Use Next");
                        LogTool.Log("Current " + current.E);
                        LogTool.Log("Next " + next.E);

                        p.MoveToNext();
                        p.temperature *= p.alpha;

                        next = p.Next as FishStateData;
                    }
                    next.UpdateNewValue();
                    next.simulator.TryToRun();
                }

                return sol;
            }
        }

        [SerializeField] protected FishSA fishSA;
        [SerializeField] protected FishSA.FishProblem problem;

        protected void Start()
        {
            problem = new FishSA.FishProblem() {temperature = 1, minTemperature = 0.0001f, alpha = 0.99f};
            var dt = new FishSimulator.Delta();
            this.fishSA = new FishSA(problem, dt);

            this.fishSA.StartSA();

            this.UpdateCurves();
        }

        protected void OnDisable()
        {
            this.fishSA.StopThread();
        }

        protected void UpdateCurves()
        {
            this.curves.Clear();

            foreach (var d in (problem.Current as FishSA.FishStateData).activations.Values)
            {
                this.curves.Add(d.ToAnimationCurve());
            }
            foreach (var d in (problem.Next as FishSA.FishStateData).activations.Values)
            {
                this.curves.Add(d.ToAnimationCurve());
            }
        }
        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.U)) this.UpdateCurves();
        }

        protected void OnDrawGizmos()
        {
            (this.problem?.Next as FishSA.FishStateData)?.problem?.FishData?.OnGizmos(GeometryFunctions.springColorMap);
        }
    }
}