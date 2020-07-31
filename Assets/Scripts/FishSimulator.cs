using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityTools;
using UnityTools.Common;
using UnityTools.Math;

namespace UnityFishSimulation
{
    public class FishSimulator : Simulator<FishSimulator.Output, FishSimulator.Runner>
    {
        public enum RunMode
        {
            PerStep,
            FullInterval,
        }
        public enum SolverType
        {
            Eular,
            Matrix,
        }
        public class Runner : IRunable<Output>
        {
            protected DiscreteFunction<float, float3> trajactory;
            protected DiscreteFunction<float, float3> velocity;
            protected float current = 0;
            protected int currentIndex = 0;
            protected FishModelData fish;
            protected FishSolver solver;

            public Runner(FishModelData fish, FishSolver solver, float from, float to)
            {
                this.fish = fish;
                this.solver = solver;

                var start = new Tuple<float, float3>(from, 0);
                var end = new Tuple<float, float3>(to, 0);
                this.trajactory = new DiscreteFunction<float, float3>(start, end, 30);
                this.velocity = new DiscreteFunction<float, float3>(start, end, 30);

                this.current = 0;
                this.currentIndex = 0;
            }

            public void Start()
            {
                this.trajactory.ResetValues();
                this.velocity.ResetValues();
                this.current = 0;
                this.currentIndex = 0;
            }
            public void End()
            {
            }

            public void Step(float dt)
            {
                this.solver.Step(this.fish, dt);
                this.current += dt;
                this.UpdateOutput();
            }

            public Output GetOutput()
            {
                return new Output() { trajactory = this.trajactory ,velocity = this.velocity};
            }


            protected void UpdateOutput()
            {
                if(this.current > this.trajactory.GetValueX(this.currentIndex))
                {
                    this.trajactory.SetValueY(this.currentIndex, this.fish.Head.Position);
                    this.velocity.SetValueY(this.currentIndex, this.fish.Velocity);
                    this.currentIndex++;
                }
            }
        }

        public class Output
        {
            public DiscreteFunction<float, float3> trajactory;
            public DiscreteFunction<float, float3> velocity;
        }


        public class FishSimulatorRunningState : SimulatorSateRunningTimer
        {
            new public static FishSimulatorRunningState Instance { get => instance; }
            new protected static FishSimulatorRunningState instance = new FishSimulatorRunningState();
            internal override void Enter(ObjectStateMachine obj)
            {
                base.Enter(obj);

                var fishSim = obj as FishSimulator;
                fishSim.ResetData();
            }
            internal override void Excute(ObjectStateMachine obj)
            {
                var fishSim = obj as FishSimulator;
                var dt = Solver.dt;
                this.ApplyControlParameter(fishSim);

                fishSim.runner?.Step(dt);
                this.current += dt;

                if (this.current > fishSim.to) fishSim.ChangeState(SimulatorSateDone.Instance);
            }

            protected void ApplyControlParameter(FishSimulator obj)
            {
                this.ApplyByType(obj.fish, Spring.Type.MuscleFront, obj.activations);
                this.ApplyByType(obj.fish, Spring.Type.MuscleMiddle, obj.activations);
                this.ApplyByType(obj.fish, Spring.Type.MuscleBack, obj.activations);
            }
            protected void ApplyByType(FishModelData fish, Spring.Type type, Dictionary<Spring.Type, MotorController.RandomX2FDiscreteFunction> activations)
            {
                var t = this.current;
                var muscle = fish.GetSpringByType(new List<Spring.Type>() { type });
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
        }

        [SerializeField] protected RunMode runMode = RunMode.PerStep;
        [SerializeField] protected SolverType solverType = SolverType.Eular;

        [SerializeField] protected FishModelData fish;
        [SerializeField] protected FishSolver solver;
        [SerializeField] internal protected float from = 0;
        [SerializeField] internal protected float to = 0;

        internal protected Dictionary<Spring.Type, MotorController.RandomX2FDiscreteFunction> activations;

        public FishModelData Fish 
        {
            get
            {
                this.fish = this.fish??GeometryFunctions.Load();
                return this.fish;
            }
        }

        public void StartSimulation()
        {
            this.ChangeState(this.Running);
        }

        public bool IsSimulationDone()
        {
            return this.currentState == this.Done;
        }

        public Output GetOutput() 
        {
            return this.runner?.GetOutput();
        }

        public void SetStartEnd(float from, float to) 
        {
            this.from = from;
            this.to = to;
        }

        public void SetActivations(Dictionary<Spring.Type, MotorController.RandomX2FDiscreteFunction> activations)
        {
            this.activations = activations;
        }

        public override SimulatorSateRunning Running { get => FishSimulatorRunningState.Instance; }

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
            this.runner = new Runner(this.fish, this.solver, this.from, this.to);
        }
    }
}