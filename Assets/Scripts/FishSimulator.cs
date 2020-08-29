using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Algorithm;
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
            protected FishModelData fish;
            protected FishActivationData activations;

            public FishModelData FishData { get => this.fish; }
            public float From { get => this.activations.Activations.FirstOrDefault().Value.Start.Item1; }
            public float To { get => this.activations.Activations.FirstOrDefault().Value.End.Item1; }
            public int SampleNum { get => this.activations.Activations.FirstOrDefault().Value.SampleNum; }

            public Problem(FishActivationData.Type type)
            {
                this.activations = FishActivationData.Load(type.ToString());
                this.ReloadData();
            }
            public Problem(FishActivationData activations)
            {
                this.activations = activations;
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
                var activations = this.activations.Activations;

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

            internal protected bool IsTimePassed(float time)
            {
                return time > this.To;
            }
        }
        [Serializable]
        public class Solution : ISolution
        {
            internal protected DiscreteFunction<float, float3> trajactory;
            internal protected DiscreteFunction<float, float3> velocity;
            protected int currentIndex;

            public Solution(float from, float to, int sampleNum)
            {
                var start = new Tuple<float, float3>(from, float3.zero);
                var end = new Tuple<float, float3>(to, float3.zero);
                trajactory = new DiscreteFunction<float, float3>(start, end, sampleNum);
                velocity = new DiscreteFunction<float, float3>(start, end, sampleNum);
                currentIndex = 0;
            }

            public void UpdateSolution(Problem problem, Delta dt)
            {
                if (dt.current > this.trajactory.GetValueX(this.currentIndex))
                {
                    this.trajactory[this.currentIndex] = problem.FishData.Head.Position;
                    this.velocity[this.currentIndex] = problem.FishData.Velocity;
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

        public override void TryToRun()
        {
            this.ResetData();
            base.TryToRun();
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

            this.currentSolution = new Solution(p.From, p.To, p.SampleNum);
        }

        public bool IsSimulationDone()
        {
            return this.currentState == this.Done;
        }         

        public override bool IsSolutionAcceptable(ISolution solution)
        {
            var d = this.dt as Delta;
            var p = this.problem as Problem;
            return p.IsTimePassed(d.current);
        }

        public override ISolution Solve(IProblem problem)
        {
            var p = problem as Problem;
            var d = this.dt as Delta;
            var sol = this.CurrentSolution as Solution;
            p.ApplyActivations(Spring.Type.MuscleFront, dt);
            p.ApplyActivations(Spring.Type.MuscleMiddle, dt);
            p.ApplyActivations(Spring.Type.MuscleBack, dt);

            //Step fish data once
            this.solver.Solve(new FishStructureProblem() { fish = p.FishData, dt = Delta.dt });

            sol.UpdateSolution(p, d);
            
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