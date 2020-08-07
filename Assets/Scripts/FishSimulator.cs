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
        public enum RunMode
        {
            PerStep,
            FullInterval,
        }

        [Serializable]
        public class Problem : IProblem
        {
            public enum SolverType
            {
                Eular,
                Matrix,
            }
            //[SerializeField] protected RunMode runMode = RunMode.PerStep;
            protected SolverType solverType = SolverType.Eular;

            protected FishModelData fish;
            protected FishSolver solver;

            protected Dictionary<Spring.Type, FishSAOptimizer.FishSA.RandomX2FDiscreteFunction> activations;

            public FishModelData FishData { get => this.fish; }
            public float From { get => this.activations.FirstOrDefault().Value.Start.Item1; }
            public float To { get => this.activations.FirstOrDefault().Value.End.Item1; }
            public int SampleNum { get => this.activations.FirstOrDefault().Value.SampleNum; }

            public Problem(SolverType type, Dictionary<Spring.Type, FishSAOptimizer.FishSA.RandomX2FDiscreteFunction> activations)
            {
                this.solverType = type;
                this.activations = activations;
            }

            protected internal void ResetData()
            {
                this.fish = GeometryFunctions.Load().DeepCopy();
                if (this.solverType == SolverType.Eular)
                {
                    this.solver = new FishEularSolver();
                }
                else
                {
                    this.solver = new FishMatrixSolver();
                }
            }

            internal protected void Step()
            {
                //TODO move solver to FishSimulator class
                this.solver.Step(this.fish, Delta.dt);
            }

            internal protected void ApplyActivations(Spring.Type type, IDelta dt)
            {
                var t = (dt as Delta).current;
                var muscle = this.fish.GetSpringByType(new List<Spring.Type>() { type });
                var muscleLeft = muscle.Where(s => s.SpringSide == Spring.Side.Left);
                var muscleRight = muscle.Where(s => s.SpringSide == Spring.Side.Right);

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


        public FishSimulator(IProblem problem, IDelta dt) : base(problem, dt)
        {

        }

        public void StartSimulation()
        {
            this.ResetData();
            this.ChangeState(this.Running);
        }
        public void ResetData()
        {
            var p = this.problem as Problem;
            p.ResetData();

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

            p.Step();

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