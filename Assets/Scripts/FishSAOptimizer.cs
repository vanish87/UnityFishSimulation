using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm;
using UnityTools.Common;
using UnityTools.Debuging;
using UnityTools.Debuging.EditorTool;
using UnityTools.Math;

namespace UnityFishSimulation
{
    public class FishSAOptimizer : MonoBehaviour
    {

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
            [System.Serializable]
            public class RandomX2FDiscreteFunction : X2FDiscreteFunction<float>
            {
                public RandomX2FDiscreteFunction(Tuple<float, float> start, Tuple<float, float> end, int sampleNum) : base(start, end, sampleNum)
                {

                }

                public void RandomCurrentValues()
                {
                    var range = 1;
                    var current = this.valueMap.Current;
                    for (var i = 0; i < current.Count; ++i)
                    {
                        var n = current[i].Item1;
                        var y = current[i].Item2;
                        current[i] = new Tuple<float, float>(n, math.saturate(y + (ThreadSafeRandom.NextFloat() - 0.5f) * 2 * range));
                    }
                }
                public override void RandomNextValues()
                {
                    var range = 1;
                    for (var i = 0; i < this.valueMap.Next.Count; ++i)
                    {
                        var n = this.valueMap.Next[i].Item1;
                        var y = this.valueMap.Next[i].Item2;
                        this.valueMap.Next[i] = new Tuple<float, float>(n, math.saturate(y + (ThreadSafeRandom.NextFloat() - 0.5f) * 2 * range));
                    }
                }
            }

            public class FishStateData: State
            {
                public Dictionary<Spring.Type, RandomX2FDiscreteFunction> activations = new Dictionary<Spring.Type, RandomX2FDiscreteFunction>();
                public FishSimulator simulator;
                public FishSimulator.Problem problem;

                public float2 timeInterval = new float2(0, 50);
                public int sampleSize = 30;
                public float h;

                public float E
                {
                    get
                    {
                        var sol = this.simulator.CurrentSolution;
                        return GetCurrentE(this.activations, this.sampleSize, this.h);
                    }
                }

                public void UpdateNewValue()
                {
                    foreach(var act in this.activations)
                    {
                        act.Value.RandomCurrentValues();
                    }
                }

                public FishStateData()
                {
                    this.h = (this.timeInterval.y - this.timeInterval.x) / sampleSize;
                    var start = new Tuple<float, float>(this.timeInterval.x, 0.5f);
                    var end = new Tuple<float, float>(this.timeInterval.y, 0.5f);

                    //this.activations.Add(Spring.Type.MuscleFront, new X2FDiscreteFunction<float>(start, end, this.sampleSize));
                    this.activations.Add(Spring.Type.MuscleMiddle, new RandomX2FDiscreteFunction(start, end, this.sampleSize));
                    this.activations.Add(Spring.Type.MuscleBack, new RandomX2FDiscreteFunction(start, end, this.sampleSize));

                    problem = new FishSimulator.Problem(FishSimulator.Problem.SolverType.Eular, this.activations);
                    var delta = new FishSimulator.Delta();

                    this.simulator = new FishSimulator(problem, delta);
                }
            }
            public class FishState : CricleData<FishStateData, int>
            {
                public FishState():base(2)
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

                public override State Current => this.state.Current;

                public override State Next => this.state.Next;


                public override void MoveToNext()
                {
                    this.state.MoveToNext();
                }

                public float3 targetPoint;
            }
            public class FishSASolution
            {
                public Dictionary<Spring.Type, RandomX2FDiscreteFunction> actvations;
            }


            public static float GetCurrentE(Dictionary<Spring.Type, RandomX2FDiscreteFunction> activations, int sampleSize, float h)
            {
                var mu1 = 0.8f;
                var mu2 = 0.2f;

                var v1 = 0.01f;
                var v2 = 1f;

                var E = 0f;

                /*var trajactory = this.fishSimulator.GetOutput()?.trajactory;
                var velocity = this.fishSimulator.GetOutput()?.velocity;*/

                for (int i = 0; i < sampleSize; ++i)
                {
                    var Eu = 0f;
                    //var Ev = this.mode == ControllerMode.LearningWithSimulation?
                    //    math.length(trajactory.Evaluate(i) - new float3(100,0,0)) - velocity.Evaluate(i).x
                    //    :0;
                    var Ev = 0;


                    var du = 0f;
                    var du2 = 0f;
                    foreach (var fun in activations.Values)
                    {
                        var dev = fun.Devrivate(i, h);
                        var dev2 = fun.Devrivate2(i, h);
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

                current.simulator.StartSimulation();
                next.simulator.StartSimulation();

                this.ChangeState(this.Running);
            }

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
                        //TODO
                        //Update current sol;
                    }

                    next.UpdateNewValue();
                    next.simulator.StartSimulation();
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
        }

        protected void OnDrawGizmos()
        {
            (this.problem?.Next as FishSA.FishStateData)?.problem?.FishData?.OnGizmos(GeometryFunctions.springColorMap);
        }
    }
}