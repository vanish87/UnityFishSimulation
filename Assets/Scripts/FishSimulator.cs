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
            
            internal protected virtual void ApplyActivations(Spring.Type type, IDelta dt)
            {
                var t = (dt as Delta).current;
                var muscle = this.fish.GetSpringByType(new List<Spring.Type>() { type });
                var muscleLeft = muscle.Where(s => s.SpringSide == Spring.Side.Left);
                var muscleRight = muscle.Where(s => s.SpringSide == Spring.Side.Right);

                var data = this.Current;
                LogTool.LogAssertIsTrue(data != null, "fishActivationDatas is empty");

                var activations = data.Activations;

                if (activations.ContainsKey(type))
                {
                    var activation = activations[type];

                    foreach (var l in muscleLeft)
                    {
                        //l.Activation = act;
                        //l.Activation = cos;// 
                        l.Activation = activation.Evaluate(t);
                    }
                    foreach (var r in muscleRight)
                    {
                        //r.Activation = 1 - act;
                        //r.Activation = 1 - cos;// 
                        r.Activation = 1 - activation.Evaluate(t);
                    }
                }
            }

            internal protected void Update(Solution solution, Delta dt)
            {
                var data = this.Current;
                this.IsDone = dt.current > data.Activations.Values.First().End.Item1;
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
                var act = p.Current.Activations.Values.First();
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


        [SerializeField] protected List<MassPoint> runtimeList;
        [SerializeField] protected List<Spring> runtimeMuscleList;
        [SerializeField] protected List<Spring> runtimeSpringList;

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
            var p = problem as Problem;
            var d = this.dt as Delta;
            var s = this.CurrentSolution as Solution;
            p.ApplyActivations(Spring.Type.MuscleFront, dt);
            p.ApplyActivations(Spring.Type.MuscleMiddle, dt);
            p.ApplyActivations(Spring.Type.MuscleBack, dt);

            //Step fish data once
            this.solver.Solve(new FishStructureProblem() { fish = p.FishData, dt = Delta.dt });

            p.Update(s, d);
            s.Update(p, d);
            
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