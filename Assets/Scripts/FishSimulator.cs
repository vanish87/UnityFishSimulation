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
    public class FishController
    {
        public enum SolverType
        {
            Euler,
            Matrix,
        }
        protected FishBody body;
        protected FishBrain brain;

        internal protected DiscreteFunction<float, float3> trajactory;
        internal protected DiscreteFunction<float, float3> velocity;
        internal protected int currentIndex;

        protected SolverType solverType = SolverType.Euler;
        protected IAlgorithm solver;

        [SerializeField] public List<MassPoint> runtimeList;
        [SerializeField] public List<Spring> runtimeMuscleList;
        [SerializeField] public List<Spring> runtimeSpringList;
        [SerializeField] public List<FinFace> runtimeFinList;

        public bool IsDone { get; set; }

        public FishActivationData Current { get { return this.fishActivationDatas.Peek(); } }
        protected Queue<FishActivationData> fishActivationDatas = new Queue<FishActivationData>();

        public FishController(FishBody body, FishBrain brain)
        {
            this.body = body;
            this.brain = brain;

            this.Reset();
        }
        public void Reset()
        {
            this.body.modelData = GeometryFunctions.Load();

            if (this.solverType == SolverType.Euler)
            {
                this.solver = new FishEulerSolver();
            }
            else
            {
                this.solver = new FishMatrixSolver();
            }

            this.runtimeList = this.body.modelData.FishGraph.Nodes.ToList();
            this.runtimeSpringList = this.body.modelData.FishGraph.Edges.ToList();
            this.runtimeMuscleList = this.body.modelData.GetSpringByType(
                                                new List<Spring.Type>{
                                                    Spring.Type.MuscleBack,
                                                    Spring.Type.MuscleMiddle,
                                                    Spring.Type.MuscleFront }
                                                );

            this.runtimeFinList = this.body.modelData.FishPectoralFins;
        }

        protected float ApplyTuning(TuningData.SpringToData data, float value)
        {
            value = math.clamp((value - 0.5f) * 2 * data.amplitude, -1, 1);
            value = (value + 1) * 0.5f;
            value += data.offset;
            value = math.saturate(value);
            return value;
        }
        protected void ApplyActivations(FishSimulator.Delta delta)
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
        protected void UpdateBrain(FishSimulator.Delta delta)
        {
            this.brain.Update(FishSimulator.Delta.dt);
            if (this.fishActivationDatas.Count == 0) this.fishActivationDatas.Enqueue(this.brain.temp);
        }

        protected void UpdateBody(FishSimulator.Delta delta)
        {
            this.solver.Solve(new FishStructureProblem() { fish = body.modelData, dt = FishSimulator.Delta.dt });
        }

        protected void UpdateSolution(FishSimulator.Delta delta, FishSimulator.Solution solution)
        {
            var data = this.Current;
            this.IsDone = delta.current > data.ToDiscreteFunctions().First().End.Item1;
            if (this.IsDone && this.fishActivationDatas.Count > 1)
            {
                this.fishActivationDatas.Dequeue();
            }

            if (this.IsDone) delta.Reset();

            return;
            if (delta.current > this.trajactory.GetValueX(this.currentIndex))
            {
                this.trajactory[this.currentIndex] = this.body.modelData.Head.Position;
                this.velocity[this.currentIndex] = this.body.modelData.Velocity;
                this.currentIndex++;
            }
        }

        public void MainUpdate(FishSimulator.Delta delta, FishSimulator.Solution solution)
        {
            this.UpdateBrain(delta);
            this.ApplyActivations(delta);
            this.UpdateBody(delta);
            this.UpdateSolution(delta, solution);
        }
    }
    [Serializable]
    public class FishSimulator : Simulator
    {
        /*public static ControllerProblem FishControllerProblem => fishSimulator.problem as ControllerProblem;
        public static FishSimulator Instance => fishSimulator;
        protected static FishSimulator fishSimulator = new FishSimulator(new ControllerProblem(), new Delta());*/

        [Serializable]
        public class ControllerProblem: IProblem
        {
            public LinkedList<FishController> CurrentQueue => this.fishControllers;
            protected LinkedList<FishController> fishControllers = new LinkedList<FishController>();

            public void AddController(FishController controller)
            {
                LogTool.AssertIsFalse(this.CurrentQueue.Contains(controller));
                this.CurrentQueue.AddLast(controller);
            }
        }    
        [Serializable]
        public class Solution : ISolution
        {

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

        public FishSimulator(
            IProblem problem, 
            IDelta dt, 
            IterationAlgorithmMode mode = IterationAlgorithmMode.FullStep) : base(problem, dt, mode)
        {
        }     

        public override bool IsSolutionAcceptable(ISolution solution)
        {
            return false;
        }

        public override ISolution Solve(IProblem problem)
        {
            var d = this.dt as Delta;
            var s = this.CurrentSolution as Solution;
            var p = problem as ControllerProblem;
            foreach (var c in p.CurrentQueue)
            {
                c.MainUpdate(d, s);
            }

            return this.CurrentSolution;
        }

        public void ResetAndRun()
        {
            var p = problem as ControllerProblem;
            foreach (var c in p.CurrentQueue)
            {
                c.Reset();
            }

            this.Reset();
            this.TryToRun();
        }
    }
}