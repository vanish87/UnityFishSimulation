using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Algorithm;
using UnityTools.Debuging;
using UnityTools.Math;

namespace UnityFishSimulation
{
    [Serializable]
    public class FishSimulator : Simulator
    {
        public enum SolverType
        {
            Euler,
            Matrix,
        }

        [Serializable]
        public class FishController : IProblem
        {
            protected FishBody body;
            protected FishBrain brain;

            protected SolverType solverType = SolverType.Euler;
            protected IAlgorithm solver;

            protected FishActivationData Current;
            protected float ApplyTuning(TuningData.SpringToData data, float value)
            {
                value = math.clamp((value - 0.5f) * 2 * data.amplitude, -1, 1);
                value = (value + 1) * 0.5f;
                value += data.offset;
                value = math.saturate(value);
                return value;
            }
            public void ApplyActivations(Delta delta)
            {
                //get motor controller from brain
                var types = new List<Spring.Type>() { Spring.Type.MuscleFront, Spring.Type.MuscleMiddle, Spring.Type.MuscleBack };

                foreach (var type in types)
                {
                    var t = delta.current;
                    var muscle = this.body.modelData.GetSpringByType(new List<Spring.Type>() { type });
                    var muscleLeft = muscle.Where(s => s.SpringSide == Spring.Side.Left);
                    var muscleRight = muscle.Where(s => s.SpringSide == Spring.Side.Right);

                    var data = this.Current;
                    LogTool.LogAssertIsTrue(data != null, "fishActivationDatas is empty");

                    if (data.HasType(type))
                    {
                        var tuning = data.Tuning.GetDataByType(type);
                        var value = data.Evaluate(t * tuning.frequency, type, data.Tuning.useFFT);
                        //value = data.Evaluate(t * tuning.frequency, type, false);
                        var lvalue = this.ApplyTuning(tuning, value);
                        var rvalue = this.ApplyTuning(tuning, 1 - value);

                        foreach (var l in muscleLeft)
                        {
                            //l.Activation = act;
                            //l.Activation = cos;// 
                            l.Activation = lvalue;
                        }
                        foreach (var r in muscleRight)
                        {
                            //r.Activation = 1 - act;
                            //r.Activation = 1 - cos;// 
                            r.Activation = rvalue;
                        }
                    }
                }
            }

            public void UpdateBody(Delta delta)
            {
                this.solver.Solve(new FishStructureProblem() { fish = body.modelData, dt = Delta.dt });
            }

            public void Reset()
            {
                //this.body.Reset();
            }
        }

        [Serializable]
        public class Problem : IProblem
        {
            public FishModelData FishData { get => this.fish; }
            public FishActivationData Current { get { return this.fishActivationDatas.Peek(); } }
            protected FishModelData fish;
            protected Queue<FishActivationData> fishActivationDatas = new Queue<FishActivationData>();

            internal protected bool IsDone { get; set; }

            public Problem()
            {
                this.fishActivationDatas.Enqueue(FishActivationData.Load());
                this.ReloadData();
            }
            public Problem(FishActivationData activations)
            {
                this.fishActivationDatas.Enqueue(activations);
                this.ReloadData();
            }

            public virtual void ReloadData()
            {
                this.fish = GeometryFunctions.Load();
            }

            protected float ApplyTuning(TuningData.SpringToData data, float value)
            {
                value = math.clamp((value - 0.5f) * 2 * data.amplitude, -1, 1);
                value = (value + 1) * 0.5f;
                value += data.offset;
                value = math.saturate(value);
                return value;
            }
            
            internal protected virtual void ApplyActivations(Spring.Type type, IDelta dt)
            {
                var t = (dt as Delta).current;
                var muscle = this.fish.GetSpringByType(new List<Spring.Type>() { type });
                var muscleLeft = muscle.Where(s => s.SpringSide == Spring.Side.Left);
                var muscleRight = muscle.Where(s => s.SpringSide == Spring.Side.Right);

                var data = this.Current;
                LogTool.LogAssertIsTrue(data != null, "fishActivationDatas is empty");

                if (data.HasType(type))
                {
                    var tuning = data.Tuning.GetDataByType(type);
                    var value = data.Evaluate(t * tuning.frequency, type, data.Tuning.useFFT);
                    //value = data.Evaluate(t * tuning.frequency, type, false);
                    var lvalue = this.ApplyTuning(tuning, value);
                    var rvalue = this.ApplyTuning(tuning, 1 - value);

                    foreach (var l in muscleLeft)
                    {
                        //l.Activation = act;
                        //l.Activation = cos;// 
                        l.Activation = lvalue;
                    }
                    foreach (var r in muscleRight)
                    {
                        //r.Activation = 1 - act;
                        //r.Activation = 1 - cos;// 
                        r.Activation = rvalue;
                    }
                }
            }

            internal protected void Update(Solution solution, Delta dt)
            {
                var data = this.Current;
                this.IsDone = dt.current > data.ToDiscreteFunctions().First().End.Item1;
                if (this.IsDone && this.fishActivationDatas.Count > 1)
                {
                    this.fishActivationDatas.Dequeue();
                }
            }
        }
        [Serializable]
        public class Solution : ISolution
        {
            internal protected DiscreteFunction<float, float3> trajactory;
            internal protected DiscreteFunction<float, float3> velocity;
            protected int currentIndex;

            public Solution(Problem p)
            {
                var act = p.Current.ToDiscreteFunctions().First();
                var start = new Tuple<float, float3>(act.Start.Item1, float3.zero);
                var end = new Tuple<float, float3>(act.End.Item1, float3.zero);
                trajactory = new DiscreteFunction<float, float3>(start, end, act.SampleNum);
                velocity = new DiscreteFunction<float, float3>(start, end, act.SampleNum);
                currentIndex = 0;
            }

            internal protected void Update(Problem p, Delta dt)
            {
                if (dt.current > this.trajactory.GetValueX(this.currentIndex))
                {
                    this.trajactory[this.currentIndex] = p.FishData.Head.Position;
                    this.velocity[this.currentIndex] = p.FishData.Velocity;
                    this.currentIndex++;
                }
            }
        }

        [Serializable]
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

        protected SolverType solverType = SolverType.Euler;
        protected IAlgorithm solver;


        [SerializeField] public List<MassPoint> runtimeList;
        [SerializeField] public List<Spring> runtimeMuscleList;
        [SerializeField] public List<Spring> runtimeSpringList;
        [SerializeField] public List<FinFace> runtimeFinList;

        public FishSimulator(
            SolverType type, 
            IProblem problem, 
            IDelta dt, 
            IterationAlgorithmMode mode = IterationAlgorithmMode.FullStep) : base(problem, dt, mode)
        {
            this.solverType = type; 
        }

        public void ResetAndRun()
        {
            this.ResetData();
            this.TryToRun();
        }

        public void ResetData()
        {
            var p = this.problem as Problem;
            p.ReloadData();

            this.runtimeList        = p.FishData.FishGraph.Nodes.ToList();
            this.runtimeSpringList  = p.FishData.FishGraph.Edges.ToList();
            this.runtimeMuscleList  = p.FishData.GetSpringByType(
                                                new List<Spring.Type>{
                                                    Spring.Type.MuscleBack,
                                                    Spring.Type.MuscleMiddle,
                                                    Spring.Type.MuscleFront }
                                                );

            this.runtimeFinList = p.FishData.FishPectoralFins;

            if (this.solverType == SolverType.Euler)
            {
                this.solver = new FishEulerSolver();
            }
            else
            {
                this.solver = new FishMatrixSolver();
            }

            this.currentSolution = new Solution(p);
        }

        public bool IsSimulationDone()
        {
            return this.currentState == this.Done;
        }         

        public override bool IsSolutionAcceptable(ISolution solution)
        {
            return (this.problem as Problem).IsDone;
        }

        public override ISolution Solve(IProblem problem)
        {
            var useNew = problem is FishController;
            var d = this.dt as Delta;
            var s = this.CurrentSolution as Solution;
            if (useNew)
            {
                var p = problem as FishController;

                p.ApplyActivations(d);
                p.UpdateBody(d);
                //s.Update(p, d);
            }
            else
            {
                var p = problem as Problem;

                p.ApplyActivations(Spring.Type.MuscleFront, dt);
                p.ApplyActivations(Spring.Type.MuscleMiddle, dt);
                p.ApplyActivations(Spring.Type.MuscleBack, dt);

                //Step fish data once
                this.solver.Solve(new FishStructureProblem() { fish = p.FishData, dt = Delta.dt });

                p.Update(s, d);
                s.Update(p, d);
            }

            
            return this.CurrentSolution;
        }

        public void OnGizmos()
        {
            if (this.problem != null)
            {
                var p = this.problem as Problem;
                p.FishData?.OnGizmos(GeometryFunctions.springColorMap);
            }
        }
    }
}